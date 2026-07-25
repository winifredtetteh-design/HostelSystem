using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HostelSystem.Data;

namespace HostelSystem.Pages.Complaints
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _db;

        public DetailsModel(AppDbContext db)
        {
            _db = db;
        }

        public Complaint Complaint { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            
            Complaint = await _db.Complaints
                .Include(c => c.StatusHistories
                    .OrderBy(s => s.ChangedAt))
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (Complaint == null)
            {
                return RedirectToPage("/Complaints/MyComplaints");
            }

            return Page();
        }
    }
}