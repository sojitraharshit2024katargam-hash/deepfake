using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace DEEPFAKE.Controllers
{
    [ApiController]
    [Route("api/video")]
    public class VideoController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        // 🔥 PUT YOUR REAL NGROK HTTPS URL HERE (NO TRAILING SLASH)
        private const string BaseUrl = "https://sobersided-frank-restrainedly.ngrok-free.dev";

        public VideoController(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient();
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateVideo([FromBody] PromptRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
                return BadRequest("Prompt required");

            int baseSeed = new Random().Next();

            string rootPath = Directory.GetCurrentDirectory();
            string framesPath = Path.Combine(rootPath, "wwwroot", "frames");
            string videosPath = Path.Combine(rootPath, "wwwroot", "videos");

            Directory.CreateDirectory(framesPath);
            Directory.CreateDirectory(videosPath);

            // Clean old frames
            foreach (var file in Directory.GetFiles(framesPath))
                System.IO.File.Delete(file);

            int totalFrames = 4; // Start small for testing

            for (int i = 0; i < totalFrames; i++)
            {
                var workflow = BuildWorkflow(request.Prompt, baseSeed + i);

                var content = new StringContent(
                    JsonSerializer.Serialize(workflow),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"{BaseUrl}/prompt", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return StatusCode(500, $"ComfyUI prompt error: {error}");
                }

                var body = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(body);

                if (!doc.RootElement.TryGetProperty("prompt_id", out var idElement))
                    return StatusCode(500, "Invalid ComfyUI response");

                string? promptId = idElement.GetString();

                if (string.IsNullOrEmpty(promptId))
                    return StatusCode(500, "Prompt ID missing");

                string? filename = await WaitForImage(promptId);

                if (filename == null)
                    return StatusCode(500, $"Frame {i} generation timeout");

                byte[] imageBytes =
                    await _httpClient.GetByteArrayAsync($"{BaseUrl}/view?filename={filename}");

                string frameFile =
                    Path.Combine(framesPath, $"frame_{i:D3}.png");

                await System.IO.File.WriteAllBytesAsync(frameFile, imageBytes);

                await Task.Delay(1000); // prevent overload
            }

            string outputVideoName = $"video_{Guid.NewGuid()}.mp4";
            string outputVideoPath = Path.Combine(videosPath, outputVideoName);

            RunFFmpeg(framesPath, outputVideoPath);

            return Ok(new
            {
                videoUrl = $"/videos/{outputVideoName}"
            });
        }

        // ====================================================
        // WAIT FOR IMAGE
        // ====================================================
        private async Task<string?> WaitForImage(string promptId)
        {
            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(2000);

                var historyResponse =
                    await _httpClient.GetAsync($"{BaseUrl}/history/{promptId}");

                if (!historyResponse.IsSuccessStatusCode)
                    continue;

                var historyJson =
                    await historyResponse.Content.ReadAsStringAsync();

                var historyDoc =
                    JsonDocument.Parse(historyJson);

                if (!historyDoc.RootElement.TryGetProperty(promptId, out var promptNode))
                    continue;

                if (!promptNode.TryGetProperty("outputs", out var outputs))
                    continue;

                if (!outputs.TryGetProperty("8", out var node8))
                    continue;

                if (!node8.TryGetProperty("images", out var images))
                    continue;

                if (images.GetArrayLength() > 0)
                {
                    return images[0]
                        .GetProperty("filename")
                        .GetString();
                }
            }

            return null;
        }

        // ====================================================
        // FFMPEG
        // ====================================================
        private void RunFFmpeg(string framesPath, string outputPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = @"C:\ffmpeg\bin\ffmpeg.exe",  // 🔥 FULL PATH
                Arguments =
                    $"-y -framerate 8 -i \"{framesPath}\\frame_%03d.png\" " +
                    "-c:v libx264 -pix_fmt yuv420p " +
                    $"\"{outputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();
            process.WaitForExit();
        }

        // ====================================================
        // SDXL WORKFLOW
        // ====================================================
        private object BuildWorkflow(string prompt, int seed)
        {
            return new
            {
                prompt = new Dictionary<string, object>
                {
                    ["2"] = new
                    {
                        class_type = "CheckpointLoaderSimple",
                        inputs = new
                        {
                            ckpt_name = "sd_xl_base_1.0.safetensors"
                        }
                    },
                    ["3"] = new
                    {
                        class_type = "CLIPTextEncode",
                        inputs = new
                        {
                            text = prompt,
                            clip = new object[] { "2", 1 }
                        }
                    },
                    ["4"] = new
                    {
                        class_type = "CLIPTextEncode",
                        inputs = new
                        {
                            text = "blurry, distorted, low quality",
                            clip = new object[] { "2", 1 }
                        }
                    },
                    ["5"] = new
                    {
                        class_type = "EmptyLatentImage",
                        inputs = new
                        {
                            width = 768,
                            height = 768,
                            batch_size = 1
                        }
                    },
                    ["6"] = new
                    {
                        class_type = "KSampler",
                        inputs = new
                        {
                            model = new object[] { "2", 0 },
                            positive = new object[] { "3", 0 },
                            negative = new object[] { "4", 0 },
                            latent_image = new object[] { "5", 0 },
                            seed = seed,
                            steps = 20,
                            cfg = 6.5,
                            sampler_name = "euler",
                            scheduler = "normal",
                            denoise = 1
                        }
                    },
                    ["7"] = new
                    {
                        class_type = "VAEDecode",
                        inputs = new
                        {
                            samples = new object[] { "6", 0 },
                            vae = new object[] { "2", 2 }
                        }
                    },
                    ["8"] = new
                    {
                        class_type = "SaveImage",
                        inputs = new
                        {
                            images = new object[] { "7", 0 },
                            filename_prefix = "VIDEO"
                        }
                    }
                }
            };
        }
    }
}