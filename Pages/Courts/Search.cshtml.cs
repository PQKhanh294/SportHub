using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Courts
{
    public class SearchModel : PageModel
    {
        private readonly ICourtService _courtService;

        public SearchModel(ICourtService courtService)
        {
            _courtService = courtService;
        }

        [BindProperty(SupportsGet = true)]
        public string? Query { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SportId { get; set; }

        public List<CourtCardItem> Courts { get; set; } = new();

        public async Task OnGetAsync()
        {
            ViewData["ActivePage"] = "SearchCourts";

            var courts = await _courtService.SearchCourtsAsync(Query ?? string.Empty, SportId);
            Courts = courts.Select(c => new CourtCardItem
            {
                CourtId = c.CourtID,
                CourtName = string.IsNullOrWhiteSpace(c.CourtName) ? "Sport Court" : c.CourtName,
                VenueName = c.Venue.VenueName,
                SportName = c.Sport.SportName,
                PriceDisplay = c.PricingRules.Any() ? $"${c.PricingRules.Min(p => p.UnitPrice):N0}/hr" : "$25/hr",
                ImageUrl = c.Images.OrderBy(i => i.SortOrder).FirstOrDefault(i => i.IsMain)?.ImageUrl
                           ?? c.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.ImageUrl
                           ?? "https://lh3.googleusercontent.com/aida-public/AB6AXuDM84Fzfc-P87JSXiTsebvH3b1RA7qa1aymfMak9JVRfm5Tv9kPqhiDN5QtRpjSD697VKK8brO-7TLfgEM1V5iHP9qpenCEiD4OH9GK20ptqZwAEM7JfJRmYQdhQ_RmltyTDA_JCmVeuerQlGHijRBWuBZxZCqikmKhsQFVkia4D0ztgAbG9FMGsEm6zP33SCGv-7vLRcv5b3JO9tBR4Ak2QLRAEVVFIC3KOrmovQdmFqt9OFgEVzkCg-j1PGXaEBNlWDuLby7ysak"
            }).ToList();
        }

        public class CourtCardItem
        {
            public int CourtId { get; set; }
            public string CourtName { get; set; } = string.Empty;
            public string VenueName { get; set; } = string.Empty;
            public string SportName { get; set; } = string.Empty;
            public string PriceDisplay { get; set; } = string.Empty;
            public string ImageUrl { get; set; } = string.Empty;
        }
    }
}
