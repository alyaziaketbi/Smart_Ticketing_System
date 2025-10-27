using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SmartTicketingManagementApp.Data;
using SmartTicketingManagementApp.Data.Entities;
using SmartTicketingManagementApp.Models;
using System.Linq;

namespace SmartTicketingManagementApp.Pages.Requester
{
    public class IndexModel : PageModel
    {

        private readonly AppDbContext _db;
        public IndexModel(AppDbContext db) => _db = db;

        public record TicketRow(
            int Id,
            string Subject,
            string Status,
            string? Priority,
            DateTime? CreatedAt,
            string Body
        );

        public List<TicketRow> Items { get; private set; } = new();

        [BindProperty] public string Title { get; set; } = string.Empty;
        [BindProperty] public string? Description { get; set; }
        [BindProperty] public string Priority { get; set; } = "Medium";

        public async Task<IActionResult> OnGetAsync()
        {
            // Must be a Requester
            var role = HttpContext.Session.GetString("u:role");
            if (role != "Requester")
            {
                Response.Redirect("/Login");
            }

            // Logged-in user id
            var uid = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (uid is null)
                return RedirectToPage("/Login");

            // Get the Requester tickets
            Items = await _db.tickets
                .AsNoTracking()
                .Where(t => t.requester_id == uid.Value &&
                            (t.status == "OPEN" || t.status == "IN_PROGRESS"))
                .OrderByDescending(t => t.created_at)
                .Select(t => new TicketRow(
                    t.ticket_id,
                    t.subject ?? string.Empty,
                    t.status ?? string.Empty,
                    // If you have no 'priority' column, replace with: null
                    EF.Property<string?>(t, "priority"),
                    t.created_at,              // DateTime? from DB
                    t.body ?? string.Empty     // coalesce to avoid null warnings
                ))
                .Take(50)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            // must be logged in requester
            var role = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (role != "Requester")
                return RedirectToPage("/Login");

            var uid = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (uid is null)
                return RedirectToPage("/Login");

            //Validate Data fields
            if (string.IsNullOrWhiteSpace(Title))
            {
                // Re-run GET to repopulate Items, then return the page (modal will be closed)
                await OnGetAsync();
                ModelState.AddModelError(string.Empty, "Title is required.");
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Description))
            {
                // Re-run GET to repopulate Items, then return the page (modal will be closed)
                await OnGetAsync();
                ModelState.AddModelError(string.Empty, "Description is required.");
                return Page();
            }

            var entity = new ticket
            {
                subject = Title.Trim(),
                body = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                status = "OPEN",
                requester_id = uid.Value,
                created_at = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
            };

            try
            {
                _db.Entry(entity).Property("priority").CurrentValue = Priority;
            }
            catch { /* ignore if column doesn't exist */ }

            _db.tickets.Add(entity);
            await _db.SaveChangesAsync();

            // Refresh list
            return RedirectToPage();

        }
    }
}
