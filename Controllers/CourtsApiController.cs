using Microsoft.AspNetCore.Mvc;
using SportHub.Services.Interfaces;

namespace SportHub.Controllers
{
    [ApiController]
    [Route("api/courts")]
    public class CourtsApiController : ControllerBase
    {
        private readonly ICourtService _courtService;

        public CourtsApiController(ICourtService courtService)
        {
            _courtService = courtService;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] int? sportId)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Ok(Array.Empty<object>());

            var courts = await _courtService.SearchCourtsAsync(q, sportId);
            var result = courts.Take(8).Select(c => new
            {
                id      = c.CourtID,
                name    = string.IsNullOrWhiteSpace(c.CourtName) ? "Sport Court" : c.CourtName,
                venue   = c.Venue?.VenueName ?? "",
                address = c.Venue?.Address ?? "",
                sport   = c.Sport?.SportName ?? ""
            });
            return Ok(result);
        }
    }
}
