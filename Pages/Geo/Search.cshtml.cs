using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Services;

namespace SportHub.Pages.Geo
{
    public class SearchModel : PageModel
    {
        private readonly IGeocodingService _geocoding;

        public SearchModel(IGeocodingService geocoding)
        {
            _geocoding = geocoding;
        }

        public async Task<IActionResult> OnGetAsync(string q, int limit = 5)
        {
            var results = await _geocoding.SearchAsync(q, limit);
            return new JsonResult(results.Select(r => new
            {
                lat = r.Lat,
                lon = r.Lon,
                display_name = r.DisplayName,
                source = r.Source
            }));
        }
    }
}
