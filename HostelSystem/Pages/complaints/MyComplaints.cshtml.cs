using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HostelSystem.Data;
using HostelSystem.Pages.Complaints;

namespace HostelSystem.Pages.Complaints
{
   
    [Authorize]
    public class MyComplaintsModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public MyComplaintsModel(AppDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public List<Complaint> Complaints { get; set; }

        public async Task OnGetAsync()
        {
            
            var userId = _userManager.GetUserId(User);

            
            Complaints = await _db.Complaints
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.DateSubmitted)
                .ToListAsync();
        }
    }
}