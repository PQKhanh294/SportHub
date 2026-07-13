using SportHub.Models.Entities;

namespace SportHub.Services.Interfaces
{
    public interface ICommunityListingService
    {
        Task<CommunityListing> IngestAsync(string rawText, string sourceUrl, string? sourceAuthorName,
            int submittedByUserId, CancellationToken ct = default);
    }
}
