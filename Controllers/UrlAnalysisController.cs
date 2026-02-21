using DEEPFAKE.DTOs;
using DEEPFAKE.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DEEPFAKE.Controllers
{
    [ApiController]
    [Route("api/url")]
    public class UrlAnalysisController : ControllerBase
    {
        private readonly IUrlAnalysisService _service;

        public UrlAnalysisController(IUrlAnalysisService service)
        {
            _service = service;
        }

        // ANALYZE
        [HttpPost("analyze")]
        public IActionResult Analyze([FromBody] UrlAnalysisRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.Url))
                return BadRequest("URL required");

            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return Unauthorized();

            var result = _service.Analyze(req.Url, userId.Value);

            return Ok(result);
        }

        // HISTORY
        [HttpGet("history")]
        public IActionResult History(int limit = 20)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return Unauthorized();

            var data = _service.GetHistory(userId.Value, limit);

            return Ok(data);
        }

        // CLEAR
        [HttpDelete("history")]
        public IActionResult Clear()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return Unauthorized();

            _service.ClearHistory(userId.Value);

            return Ok();
        }
    }
}
