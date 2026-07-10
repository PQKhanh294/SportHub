using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Services.Interfaces;

namespace SportHub.Controllers
{
    [ApiController]
    [Route("api/community-listings")]
    public class CommunityListingApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICommunityListingService _listingService;
        private readonly IConfiguration _config;
        private readonly ILogger<CommunityListingApiController> _logger;

        public CommunityListingApiController(
            ApplicationDbContext context,
            ICommunityListingService listingService,
            IConfiguration config,
            ILogger<CommunityListingApiController> logger)
        {
            _context = context;
            _listingService = listingService;
            _config = config;
            _logger = logger;
        }

        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromBody] SubmitListingRequest request)
        {
            if (!IsApiKeyValid())
            {
                _logger.LogWarning("Community listing submit: invalid API key.");
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.RawText) || string.IsNullOrWhiteSpace(request.SourceUrl))
                return BadRequest(new { success = false, message = "Thiếu rawText hoặc sourceUrl." });

            var submitterId = _config.GetValue<int>("ChromeExtension:SubmitterUserId");
            if (submitterId <= 0)
                return StatusCode(500, new { success = false, message = "Chưa cấu hình ChromeExtension:SubmitterUserId." });

            var listing = await _listingService.IngestAsync(
                request.RawText.Trim(), request.SourceUrl.Trim(), request.SourceAuthorName?.Trim(), submitterId);

            return Ok(new
            {
                success = true,
                listingId = listing.CommunityListingID,
                parseStatus = listing.ParseStatus,
                title = listing.Title
            });
        }

        [HttpGet("check-duplicate")]
        public async Task<IActionResult> CheckDuplicate([FromQuery] string sourceUrl)
        {
            if (!IsApiKeyValid()) return Unauthorized();
            if (string.IsNullOrWhiteSpace(sourceUrl)) return Ok(new { exists = false });

            var exists = await _context.CommunityListings.AnyAsync(c => c.SourceUrl == sourceUrl);
            return Ok(new { exists });
        }

        private bool IsApiKeyValid()
        {
            var expectedKey = _config["ChromeExtension:ApiKey"];
            if (string.IsNullOrEmpty(expectedKey)) return false;

            Request.Headers.TryGetValue("X-Api-Key", out var receivedKey);
            return receivedKey == expectedKey;
        }

        public record SubmitListingRequest(string RawText, string SourceUrl, string? SourceAuthorName);
    }
}
