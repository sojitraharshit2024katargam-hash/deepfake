using System.Text;
using System.Text.Json;

namespace DEEPFAKE.Services.ImageGeneration
{
    public class ImageGeneratorService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://sobersided-frank-restrainedly.ngrok-free.dev";

        public ImageGeneratorService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GenerateImage(string promptText)
        {
            // 🔥 Build workflow dynamically
            var workflow = new
            {
                prompt = new Dictionary<string, object>
                {
                    {
                        "2", new {
                            class_type = "CheckpointLoaderSimple",
                            inputs = new { ckpt_name = "sd_xl_base_1.0.safetensors" }
                        }
                    },
                    {
                        "3", new {
                            class_type = "CLIPTextEncode",
                            inputs = new { text = promptText, clip = new object[] { "2", 1 } }
                        }
                    },
                    {
                        "4", new {
                            class_type = "CLIPTextEncode",
                            inputs = new { text = "", clip = new object[] { "2", 1 } }
                        }
                    },
                    {
                        "5", new {
                            class_type = "EmptyLatentImage",
                            inputs = new { width = 1024, height = 1024, batch_size = 1 }
                        }
                    },
                    {
                        "6", new {
                            class_type = "KSampler",
                            inputs = new {
                                model = new object[] { "2", 0 },
                                positive = new object[] { "3", 0 },
                                negative = new object[] { "4", 0 },
                                latent_image = new object[] { "5", 0 },
                                seed = new Random().Next(),
                                steps = 20,
                                cfg = 8.0,
                                sampler_name = "euler",
                                scheduler = "simple",
                                denoise = 1.0
                            }
                        }
                    },
                    {
                        "7", new {
                            class_type = "VAEDecode",
                            inputs = new {
                                samples = new object[] { "6", 0 },
                                vae = new object[] { "2", 2 }
                            }
                        }
                    },
                    {
                        "8", new {
                            class_type = "SaveImage",
                            inputs = new {
                                images = new object[] { "7", 0 },
                                filename_prefix = "WEB"
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(workflow);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{BaseUrl}/prompt", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(responseBody);

            string promptId = doc.RootElement.GetProperty("prompt_id").GetString();

            // Wait for generation
            await Task.Delay(4000);

            var historyResponse = await _httpClient.GetAsync($"{BaseUrl}/history/{promptId}");
            var historyJson = await historyResponse.Content.ReadAsStringAsync();

            return historyJson; // For now return raw JSON
        }
    }
}