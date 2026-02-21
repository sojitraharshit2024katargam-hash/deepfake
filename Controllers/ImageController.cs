using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace DEEPFAKE.Controllers
{
    [ApiController]
    [Route("api/image")]
    public class ImageController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public ImageController(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient();
        }

        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] PromptRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
                return BadRequest("Prompt required");

            var workflow = new
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
                        inputs = new { text = request.Prompt, clip = new object[] { "2", 1 } }
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
                            seed = 123456,
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

            var content = new StringContent(
                JsonSerializer.Serialize(workflow),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(
                "http://127.0.0.1:8188/prompt",
                content
            );

            var result = await response.Content.ReadAsStringAsync();

            return Ok(result);
        }
    }

    public class PromptRequest
    {
        public string Prompt { get; set; }
    }
}