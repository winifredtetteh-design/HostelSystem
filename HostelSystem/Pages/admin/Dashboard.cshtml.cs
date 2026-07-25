using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HostelSystem.Data;
using HostelSystem.Pages.Complaints;

namespace HostelSystem.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardModel(AppDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // All complaints from all students
        public List<Complaint> Complaints { get; set; }

        // Approved students
        public List<IdentityUser> Students { get; set; }

        // Students waiting for approval
        public List<IdentityUser> PendingStudents { get; set; }

        // Stats
        public int TotalComplaints { get; set; }
        public int PendingCount { get; set; }
        public int InProgressCount { get; set; }
        public int ResolvedCount { get; set; }

        public async Task OnGetAsync()
        {
            // Fetch all complaints newest first
            Complaints = await _db.Complaints
                .OrderByDescending(c => c.DateSubmitted)
                .ToListAsync();

            // Calculate stats
            TotalComplaints = Complaints.Count;
            PendingCount = Complaints.Count(c => c.Status == "Pending");
            InProgressCount = Complaints.Count(c => c.Status == "In Progress");
            ResolvedCount = Complaints.Count(c => c.Status == "Resolved");

            // Fetch approved students (in Student role)
            Students = new List<IdentityUser>(
                await _userManager.GetUsersInRoleAsync("Student"));

            // Fetch pending students (locked out, not admin)
            var allUsers = _userManager.Users.ToList();
            PendingStudents = new List<IdentityUser>();

            foreach (var u in allUsers)
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(u);
                var isAdmin = await _userManager.IsInRoleAsync(u, "Admin");

                if (!isAdmin && lockoutEnd.HasValue &&
                    lockoutEnd.Value > DateTimeOffset.UtcNow)
                {
                    PendingStudents.Add(u);
                }
            }
        }

        // Add a new student account (already approved)
        public async Task<IActionResult> OnPostAddStudentAsync(
            string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Email and password are required.";
                return RedirectToPage();
            }

            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Student");
                TempData["Success"] = $"Student account created for {email}.";
            }
            else
            {
                var errors = string.Join(", ",
                    result.Errors.Select(e => e.Description));
                TempData["Error"] = $"Could not create account: {errors}";
            }

            return RedirectToPage();
        }

        // Remove an approved student account
        public async Task<IActionResult> OnPostRemoveStudentAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "Student not found.";
                return RedirectToPage();
            }

            // Delete their complaints first
            var complaints = _db.Complaints.Where(c => c.UserId == userId);
            _db.Complaints.RemoveRange(complaints);
            await _db.SaveChangesAsync();

            await _userManager.DeleteAsync(user);
            TempData["Success"] = $"Student {user.Email} has been removed.";

            return RedirectToPage();
        }

        // Update a complaint status and add a note
        public async Task<IActionResult> OnPostUpdateStatusAsync(
            int complaintId, string newStatus, string note)
        {
            var complaint = await _db.Complaints.FindAsync(complaintId);

            if (complaint == null)
            {
                TempData["Error"] = "Complaint not found.";
                return RedirectToPage();
            }

            complaint.Status = newStatus;

            var history = new StatusHistory
            {
                ComplaintId = complaint.Id,
                Status = newStatus,
                Note = string.IsNullOrWhiteSpace(note)
                    ? $"Status updated to {newStatus}."
                    : note,
                ChangedAt = DateTime.Now
            };

            _db.StatusHistories.Add(history);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Complaint status updated.";
            return RedirectToPage();
        }

        // Approve a pending student
        public async Task<IActionResult> OnPostApproveStudentAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "Student not found.";
                return RedirectToPage();
            }

            // Unlock the account
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            // Assign Student role if not already assigned
            if (!await _userManager.IsInRoleAsync(user, "Student"))
            {
                await _userManager.AddToRoleAsync(user, "Student");
            }

            TempData["Success"] = $"{user.Email} has been approved and can now log in.";
            return RedirectToPage();
        }

        // Reject a pending student
        public async Task<IActionResult> OnPostRejectStudentAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "Student not found.";
                return RedirectToPage();
            }

            var email = user.Email;
            await _userManager.DeleteAsync(user);
            TempData["Success"] = $"{email}'s registration has been rejected and removed.";
            return RedirectToPage();
        }
    }
}
