using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;

namespace SportHub.Pages.Admin
{
    [Authorize]
    public class CommunityListingsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CommunityListingsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<CommunityListing> Listings { get; set; } = new();
        [TempData] public string? Msg { get; set; }

        [BindProperty(SupportsGet = true)] public string? FilterStatus { get; set; }
        [BindProperty(SupportsGet = true)] public string? FilterParseStatus { get; set; }

        private async Task<bool> IsAdminAsync()
        {
            var rolesClaim = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
            return rolesClaim.Contains("Admin");
        }

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActivePage"] = "Admin";
            ViewData["AdminPage"] = "CommunityListings";
            if (!await IsAdminAsync()) return Forbid();

            var query = _context.CommunityListings
                .Include(c => c.Sport)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(FilterStatus))
                query = query.Where(c => c.Status == FilterStatus);
            if (!string.IsNullOrWhiteSpace(FilterParseStatus))
                query = query.Where(c => c.ParseStatus == FilterParseStatus);

            Listings = await query.OrderByDescending(c => c.CreatedAt).Take(200).ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostHideAsync(int id)
        {
            if (!await IsAdminAsync()) return Forbid();

            var listing = await _context.CommunityListings.FindAsync(id);
            if (listing != null)
            {
                listing.Status = "Hidden";
                await _context.SaveChangesAsync();
                Msg = $"Đã ẩn tin \"{listing.Title}\".";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUnhideAsync(int id)
        {
            if (!await IsAdminAsync()) return Forbid();

            var listing = await _context.CommunityListings.FindAsync(id);
            if (listing != null && listing.ExpiresAt > DateTime.UtcNow)
            {
                listing.Status = "Active";
                await _context.SaveChangesAsync();
                Msg = $"Đã hiện lại tin \"{listing.Title}\".";
            }
            return RedirectToPage();
        }
    }
}
