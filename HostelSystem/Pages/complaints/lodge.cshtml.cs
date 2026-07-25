using HostelSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace HostelSystem.Pages.Complaints
{
    [Authorize]


    public class LodgeModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<IdentityUser> _userManager;

        public LodgeModel(AppDbContext db, IWebHostEnvironment environment,
            UserManager<IdentityUser> userManager)
        {
            _db = db;
            _environment = environment;
            _userManager = userManager;
        }

        [BindProperty]
        public ComplaintInput Input { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            string? imagePath = null;

            if (Input.Image != null && Input.Image.Length > 0)
            {
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(Input.Image.ContentType.ToLower()))
                {
                    ModelState.AddModelError("Input.Image", "Only image files are allowed.");
                    return Page();
                }

                if (Input.Image.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Input.Image", "Image must be less than 5MB.");
                    return Page();
                }

                var extension = Path.GetExtension(Input.Image.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "complaints");
                var fullPath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await Input.Image.CopyToAsync(stream);
                }

                imagePath = $"/images/complaints/{uniqueFileName}";
            }

            
            var userId = _userManager.GetUserId(User);

            var complaint = new Complaint
            {
                Title = Input.Title,
                Description = Input.Description,
                Category = Input.Category,
                RoomNumber = Input.RoomNumber,
                Status = "Pending",
                DateSubmitted = DateTime.Now,
                ImagePath = imagePath,
                UserId = userId
            };

            _db.Complaints.Add(complaint);
            await _db.SaveChangesAsync();

            var history = new StatusHistory
            {
                ComplaintId = complaint.Id,
                Status = "Pending",
                Note = "Complaint submitted successfully.",
                ChangedAt = DateTime.Now
            };

            _db.StatusHistories.Add(history);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Complaints/MyComplaints");
        }
    }

    public class ComplaintInput
    {
        [Required(ErrorMessage = "Please enter a title")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Please select a category")]
        public string Category { get; set; }

        [Required(ErrorMessage = "Please enter your room number")]
        public string RoomNumber { get; set; }

        [Required(ErrorMessage = "Please enter a description")]
        public string Description { get; set; }

        public IFormFile? Image { get; set; }
    }

    public class Complaint
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Category { get; set; }

        [Required]
        public string RoomNumber { get; set; }

        [Required]
        public string Status { get; set; }

        public DateTime DateSubmitted { get; set; }

        public string? ImagePath { get; set; }
        public string? UserId { get; set; }

        public List<StatusHistory> StatusHistories { get; set; }
            = new List<StatusHistory>();
    }

    public class StatusHistory
    {
        [Key]
        public int Id { get; set; }

        public int ComplaintId { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public string Note { get; set; }

        public DateTime ChangedAt { get; set; }

        public Complaint Complaint { get; set; }
    }
}