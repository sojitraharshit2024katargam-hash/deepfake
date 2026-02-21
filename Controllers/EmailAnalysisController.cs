using DEEPFAKE.DTOs;
using DEEPFAKE.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace DEEPFAKE.Controllers
{
    [ApiController]
    [Route("api/email")]
    public class EmailAnalysisController : ControllerBase
    {
        private readonly IEmailAnalysisService _service;

        public EmailAnalysisController(IEmailAnalysisService service)
        {
            _service = service;
        }

        // ===================== ANALYZE =====================
        [HttpPost("analyze")]
        public IActionResult Analyze([FromBody] EmailAnalysisRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.EmailContent))
                return BadRequest("EmailContent is required");

            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized("User not logged in");

            var result = _service.Analyze(request.EmailContent, userId.Value);
            return Ok(result);
        }

        // ===================== GET HISTORY =====================
        [HttpGet("history")]
        public IActionResult History([FromQuery] int limit = 20)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized("User not logged in");

            var history = _service.GetHistory(userId.Value, limit);
            return Ok(history);
        }

        // ===================== CLEAR HISTORY =====================
        [HttpDelete("history")]
        public IActionResult ClearHistory()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized("User not logged in");

            _service.ClearHistory(userId.Value);
            return Ok(new { message = "History cleared successfully" });
        }
    }
}
