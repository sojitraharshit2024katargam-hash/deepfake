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

        private const string BaseUrl = "https://2558389301f1b4.lhr.life";

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
                var workflow = BuildWorkflow(request);

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

                string imageUrl = $"{BaseUrl}/view?filename={filename}&subfolder=&type=output";

                return Ok(new { imageUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Image Generation Error: " + ex.Message);
                return StatusCode(500, "Image generation failed.");
            }
        }

        // ===============================
        // POLLING FOR RESULT
        // ===============================
        private async Task<string?> WaitForImage(string promptId)
        {
            for (int i = 0; i < 60; i++) // up to ~2 minutes
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
                    return images[0].GetProperty("filename").GetString();
            }

            return null;
        }

        // ===============================
        // BUILD WORKFLOW
        // ===============================
        private object BuildWorkflow(PromptRequest request)
        {
            int steps = 40;
            double cfg = 7;
            string sampler = "dpmpp_2m_sde";
            string scheduler = "karras";

            string model = request.Model.ToLower();

            // Lightning models
            if (model.Contains("lightning"))
            {
                steps = 8;
                cfg = 2;
                sampler = "dpmpp_sde";
                scheduler = "normal";
            }

            string positivePrompt =
                $"{request.Prompt}, masterpiece, best quality, ultra detailed, cinematic lighting, volumetric lighting, depth of field, professional photography, sharp focus";

            string negativePrompt =
                "bad anatomy, bad hands, extra fingers, extra limbs, deformed face, ugly, blurry, low quality, watermark, text, cropped, worst quality";

            return new
            {
                prompt = new Dictionary<string, object>
                {
                    ["2"] = new
                    {
                        class_type = "CheckpointLoaderSimple",
                        inputs = new
                        {
                            ckpt_name = request.Model
                        }
                    },

                    ["3"] = new
                    {
                        class_type = "CLIPTextEncode",
                        inputs = new
                        {
                            text = positivePrompt,
                            clip = new object[] { "2", 1 }
                        }
                    },

                    ["4"] = new
                    {
                        class_type = "CLIPTextEncode",
                        inputs = new
                        {
                            text = negativePrompt,
                            clip = new object[] { "2", 1 }
                        }
                    },

                    ["5"] = new
                    {
                        class_type = "EmptyLatentImage",
                        inputs = new
                        {
                            width = request.Width,
                            height = request.Height,
                            batch_size = request.Batch
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
                            seed = Random.Shared.Next(),
                            steps = steps,
                            cfg = cfg,
                            sampler_name = sampler,
                            scheduler = scheduler,
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
        public string Model { get; set; } = "sd_xl_base_1.0.safetensors";

        public int Width { get; set; } = 1024;
        public int Height { get; set; } = 1024;

        public int Batch { get; set; } = 1;
    }

}
