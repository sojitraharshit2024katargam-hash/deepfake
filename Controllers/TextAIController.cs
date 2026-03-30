using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace DEEPFAKE.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class TextAIController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TextAIController> _logger;

        private const string BaseUrl = "https://YOUR_NGROK_URL_HERE";

        public TextAIController(
            IHttpClientFactory factory,
            ILogger<TextAIController> logger)
        {
            _httpClient = factory.CreateClient();
            _logger = logger;
        }

        // ── Standard (full response) ──────────────────────────────
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] AIRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
                return BadRequest(new { error = "Prompt is required." });

            var body = new
            {
                model = request.Model ?? "llama3",
                prompt = BuildPrompt(request),
                stream = false,
                options = new
                {
                    temperature = request.Temperature ?? 0.7,
                    num_predict = request.MaxTokens ?? 2048,
                    top_p = 0.9,
                    repeat_penalty = 1.1
                }
            };

            try
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Post,
                    $"{BaseUrl}/api/generate")
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(body),
                        Encoding.UTF8,
                        "application/json")
                };

                httpRequest.Headers.Add("ngrok-skip-browser-warning", "true");

                var response = await _httpClient.SendAsync(httpRequest);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var text = doc.RootElement.GetProperty("response").GetString() ?? "";

                return Ok(new
                {
                    text,
                    model = request.Model,
                    tokenCount = EstimateTokens(text),
                    done = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ollama chat request failed");
                return StatusCode(503, new { error = "Model unavailable", detail = ex.Message });
            }
        }

        // ── Streaming (Server-Sent Events) ───────────────────────
        [HttpPost("chat/stream")]
        public async Task StreamChat([FromBody] AIRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                Response.StatusCode = 400;
                return;
            }

            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";

            var body = new
            {
                model = request.Model ?? "llama3",
                prompt = BuildPrompt(request),
                stream = true,
                options = new
                {
                    temperature = request.Temperature ?? 0.7,
                    num_predict = request.MaxTokens ?? 2048,
                    top_p = 0.9,
                    repeat_penalty = 1.1
                }
            };

            try
            {
                var reqMsg = new HttpRequestMessage(HttpMethod.Post,
                    $"{BaseUrl}/api/generate")
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(body),
                        Encoding.UTF8,
                        "application/json")
                };

                reqMsg.Headers.Add("ngrok-skip-browser-warning", "true");

                using var resp = await _httpClient.SendAsync(
                    reqMsg, HttpCompletionOption.ResponseHeadersRead, ct);

                resp.EnsureSuccessStatusCode();

                using var stream = await resp.Content.ReadAsStreamAsync(ct);
                using var reader = new System.IO.StreamReader(stream);

                while (!reader.EndOfStream && !ct.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(ct);
                    if (string.IsNullOrEmpty(line)) continue;

                    try
                    {
                        var chunk = JsonDocument.Parse(line);
                        var token = chunk.RootElement.GetProperty("response").GetString() ?? "";
                        var done = chunk.RootElement.GetProperty("done").GetBoolean();

                        var payload = JsonSerializer.Serialize(new { token, done });
                        await Response.WriteAsync($"data: {payload}\n\n", ct);
                        await Response.Body.FlushAsync(ct);

                        if (done) break;
                    }
                    catch { /* skip malformed chunk */ }
                }

                await Response.WriteAsync("data: [DONE]\n\n", ct);
            }
            catch (OperationCanceledException)
            {
                await Response.WriteAsync("data: [CANCELLED]\n\n");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ollama streaming failed");
                await Response.WriteAsync($"data: {{\"error\":\"{ex.Message}\"}}\n\n");
            }
        }

        // ── List available models ─────────────────────────────────
        [HttpGet("models")]
        public async Task<IActionResult> GetModels()
        {
            try
            {
                var reqMsg = new HttpRequestMessage(HttpMethod.Get,
                    $"{BaseUrl}/api/tags");
                reqMsg.Headers.Add("ngrok-skip-browser-warning", "true");

                var resp = await _httpClient.SendAsync(reqMsg);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var models = doc.RootElement
                    .GetProperty("models")
                    .EnumerateArray()
                    .Select(m => new
                    {
                        name = m.GetProperty("name").GetString(),
                        size = m.TryGetProperty("size", out var s) ? s.GetInt64() : 0L,
                        modified = m.TryGetProperty("modified_at", out var d) ? d.GetString() : null
                    })
                    .ToList();

                return Ok(new { models, ollamaUrl = BaseUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { error = "Could not reach Ollama", detail = ex.Message });
            }
        }

        // ── Health check ─────────────────────────────────────────
        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            try
            {
                var reqMsg = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/");
                reqMsg.Headers.Add("ngrok-skip-browser-warning", "true");

                var resp = await _httpClient.SendAsync(reqMsg);
                return Ok(new
                {
                    status = "ok",
                    ollamaReachable = resp.IsSuccessStatusCode,
                    ollamaUrl = BaseUrl
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = "degraded",
                    ollamaReachable = false,
                    ollamaUrl = BaseUrl,
                    error = ex.Message
                });
            }
        }

        // ── Helpers ───────────────────────────────────────────────
        private static string BuildPrompt(AIRequest request)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
                sb.AppendLine($"[SYSTEM]\n{request.SystemPrompt.Trim()}\n");

            if (request.History is { Length: > 0 })
            {
                foreach (var msg in request.History)
                {
                    var role = msg.Role?.ToLower() == "assistant" ? "Assistant" : "User";
                    sb.AppendLine($"{role}: {msg.Content}");
                }
            }

            sb.Append($"User: {request.Prompt.Trim()}\nAssistant:");
            return sb.ToString();
        }

        private static int EstimateTokens(string text) =>
            (int)Math.Ceiling(text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length * 1.3);
    }

    // ── DTOs ─────────────────────────────────────────────────────
    public class AIRequest
    {
        public string Prompt { get; set; } = "";
        public string? Model { get; set; }
        public string? SystemPrompt { get; set; }
        public double? Temperature { get; set; }
        public int? MaxTokens { get; set; }
        public ChatMessage[]? History { get; set; }
    }

    public class ChatMessage
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = "";
    }
}