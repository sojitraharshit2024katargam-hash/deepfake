using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace DEEPFAKE.Controllers
{
    [ApiController]
    [Route("api/image")]
    public class ImageController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        private const string BaseUrl = "https://sobersided-frank-restrainedly.ngrok-free.dev";

        public ImageController(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient();
        }

        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] PromptRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
                return BadRequest("Prompt required");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized("Please login first.");

            try
            {
                var workflow = BuildWorkflow(request.Prompt);

                var content = new StringContent(
                    JsonSerializer.Serialize(workflow),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"{BaseUrl}/prompt", content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(responseBody);

                if (!doc.RootElement.TryGetProperty("prompt_id", out var idElement))
                    return StatusCode(500, "Invalid response from ComfyUI");

                string promptId = idElement.GetString()!;

                string filename = await WaitForImage(promptId);

                if (filename == null)
                    return StatusCode(500, "Image generation timeout");

                string imageUrl = $"{BaseUrl}/view?filename={filename}";

                return Ok(new { imageUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Image Generation Error: " + ex.Message);
                return StatusCode(500, "Image generation failed.");
            }
        }

        // ==============================================
        // POLLING
        // ==============================================
        private async Task<string?> WaitForImage(string promptId)
        {
            for (int i = 0; i < 25; i++) // ~50 seconds max
            {
                await Task.Delay(2000);

                var historyResponse = await _httpClient.GetAsync($"{BaseUrl}/history/{promptId}");
                if (!historyResponse.IsSuccessStatusCode)
                    continue;

                var historyJson = await historyResponse.Content.ReadAsStringAsync();
                var historyDoc = JsonDocument.Parse(historyJson);

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
                    return images[0].GetProperty("filename").GetString();
                }
            }

            return null;
        }

        // ==============================================
        // MAX QUALITY WORKFLOW (12GB OPTIMIZED)
        // ==============================================
        private object BuildWorkflow(string prompt)
        {
            return new
            {
                prompt = new Dictionary<string, object>
                {
                    ["2"] = new
                    {
                        class_type = "CheckpointLoaderSimple",
                        inputs = new { ckpt_name = "sd_xl_base_1.0.safetensors" }
                    },

                    ["3"] = new
                    {
                        class_type = "CLIPTextEncode",
                        inputs = new
                        {
                            text = prompt + ", ultra detailed, realistic, sharp focus, cinematic lighting, 85mm lens, depth of field, high resolution, highly detailed",
                            clip = new object[] { "2", 1 }
                        }
                    },

                    ["4"] = new
                    {
                        class_type = "CLIPTextEncode",
                        inputs = new
                        {
                            text = "bad anatomy, bad hands, extra fingers, extra limbs, deformed face, ugly, mutated, low quality, blurry, distorted eyes, poorly drawn face, bad proportions, cross eyes, weird mouth",
                            clip = new object[] { "2", 1 }
                        }
                    },

                    ["5"] = new
                    {
                        class_type = "EmptyLatentImage",
                        inputs = new
                        {
                            width = 1024,
                            height = 1024,
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
                            seed = new Random().Next(),
                            steps = 35,
                            cfg = 8.5,
                            sampler_name = "dpmpp_2m_sde",
                            scheduler = "karras",
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
                            filename_prefix = "WEB"
                        }
                    }
                }
            };
        }
    }

    public class PromptRequest
    {
        public string Prompt { get; set; } = string.Empty;
    }
}