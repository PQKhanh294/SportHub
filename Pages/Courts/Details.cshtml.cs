using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Courts
{
    public class DetailsModel : PageModel
    {
        private readonly ICourtService _courtService;

        public DetailsModel(ICourtService courtService)
        {
            _courtService = courtService;
        }

        public CourtDetailItem? Item { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            ViewData["ActivePage"] = "SearchCourts";
            if (!id.HasValue)
            {
                return RedirectToPage("/Courts/Search");
            }

            var court = await _courtService.GetCourtDetailsAsync(id.Value);
            if (court == null)
            {
                return RedirectToPage("/Courts/Search");
            }

            var images = court.Images.OrderBy(i => i.SortOrder).Select(i => i.ImageUrl).ToList();
            if (images.Count == 0)
            {
                images.Add("https://lh3.googleusercontent.com/aida-public/AB6AXuAHBxb5mOtwtPLRL5iLOL3N2ia6cQPxzsMvFrnaU-mvIzkN-OFZf-WmxktE2brADSGM8XqvCXWyZX5RI0rLcjNbLPQqAA7MBNg9nWV_EgTr0tZseI1GjDxtA7uyryYCoFe30E3hS6phBiX8SyZU2lxwHdp5ukU5bdCqUM51Yh61cBE-FhONkfUQtsWW1zNn3NYcNngF09ABcjqd6YjVd6XMNMOfGvNJQSt7hTQRySVKZaCC2soLSkTSTAnkmHKoiLMdL3yB3jeOYkU");
            }

            while (images.Count < 5)
            {
                images.Add(images[0]);
            }

            var minPrice = court.PricingRules.Any() ? court.PricingRules.Min(p => p.UnitPrice) : 45;

            Item = new CourtDetailItem
            {
                CourtId = court.CourtID,
                CourtName = court.CourtName,
                VenueName = court.Venue.VenueName,
                Address = court.Venue.Address,
                Description = court.Description ?? "Premium indoor courts with modern lighting and stable playing conditions.",
                MinPriceDisplay = $"${minPrice:N0}",
                Images = images
            };

            return Page();
        }

        public class CourtDetailItem
        {
            public int CourtId { get; set; }
            public string CourtName { get; set; } = string.Empty;
            public string VenueName { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string MinPriceDisplay { get; set; } = string.Empty;
            public List<string> Images { get; set; } = new();
        }
    }
}
