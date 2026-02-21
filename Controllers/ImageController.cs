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

        // 🔥 Replace if ngrok URL changes
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

            try
            {
                var workflow = BuildWorkflow(request.Prompt);

                var content = new StringContent(
                    JsonSerializer.Serialize(workflow),
                    Encoding.UTF8,
                    "application/json"
                );

                // 🔥 STEP 1: Send prompt
                var response = await _httpClient.PostAsync($"{BaseUrl}/prompt", content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(responseBody);

                string promptId = doc.RootElement.GetProperty("prompt_id").GetString();

                // 🔥 STEP 2: Poll until ready (max 30 seconds)
                string filename = await WaitForImage(promptId);

                if (filename == null)
                    return StatusCode(500, "Image generation timeout");

                // 🔥 STEP 3: Build direct image URL
                string imageUrl = $"{BaseUrl}/view?filename={filename}";

                return Ok(new { imageUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // ====================================================
        // 🧠 POLLING METHOD (PROFESSIONAL APPROACH)
        // ====================================================
        private async Task<string> WaitForImage(string promptId)
        {
            for (int i = 0; i < 15; i++) // ~15 attempts
            {
                await Task.Delay(2000); // wait 2 seconds

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

                var images = node8.GetProperty("images");

                if (images.GetArrayLength() > 0)
                {
                    return images[0].GetProperty("filename").GetString();
                }
            }

            return null;
        }

        // ====================================================
        // 🔥 BUILD WORKFLOW
        // ====================================================
        private object BuildWorkflow(string prompt)
        {
            return new
            {
                prompt = new Dictionary<string, object>
                {
                    ["2"] = new
                    {
                        class_type = "CheckpointLoaderSimple",
                        inputs = new { ckpt_name = "v1-5-pruned-emaonly.safetensors" }
                    },
                    ["3"] = new
                    {
                        class_type = "CLIPTextEncode",
                        inputs = new { text = prompt, clip = new object[] { "2", 1 } }
                    },
                    ["4"] = new
                    {
                        class_type = "CLIPTextEncode",
                        inputs = new { text = "", clip = new object[] { "2", 1 } }
                    },
                    ["5"] = new
                    {
                        class_type = "EmptyLatentImage",
                        inputs = new { width = 512, height = 512, batch_size = 1 }
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
                            steps = 20,
                            cfg = 8,
                            sampler_name = "euler",
                            scheduler = "simple",
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
        public string Prompt { get; set; }
    }
}