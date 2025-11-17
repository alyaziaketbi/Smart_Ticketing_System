using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SmartTicketingManagementApp.Data;
using SmartTicketingManagementApp.Data.Entities;
using SmartTicketingManagementApp.Models;
using SmartTicketingManagementApp.Pages;
using SmartTicketingManagementApp.Services;
using System.Linq;


namespace SmartTicketingManagementApp.Pages.Requester
{
    public class IndexModel : PageModel
    {

        private readonly AppDbContext _db;
        private readonly ApiClient _apiClient;
        public IndexModel(AppDbContext db, ApiClient apiClient)
        {
            _db = db;
            _apiClient = apiClient;
        }

        public record TicketRow(
            int Id,
            string Subject,
            string Status,
            string? Priority,
            DateTime? CreatedAt,
            string Body,
            string? AssignedTeamId,
            string AssignedTeamName,
            string Answer
        );

        public string? UserName { get; set; }
        public string? UserRole { get; set; }

        public string? assigned_team_id { get; set; }
        public string? answer { get; set; }
        public team? assigned_team { get; set; }  // Navigation property


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

            // Get logged-in user info from session
            var current = LoginModel.GetCurrent(HttpContext);
            UserName = current?.Name ?? "User";
            UserRole = current?.Role ?? "Requester";

            // Get the Requester tickets
            Items = await _db.tickets
                .AsNoTracking()
                .Where(t => t.requester_id == uid.Value &&
                            (t.status == "OPEN" || t.status == "INPROGRESS" || t.status == "ASSIGNED" || t.status == "RESOLVED" || t.status == "CANCELED"))
                .OrderByDescending(t => t.created_at)
                .Select(t => new TicketRow(
                    t.ticket_id,
                    t.subject ?? string.Empty,
                    t.status ?? string.Empty,
                    // If you have no 'priority' column, replace with: null
                    EF.Property<string?>(t, "priority"),
                    t.created_at,              // DateTime? from DB
                    t.body ?? string.Empty,    // coalesce to avoid null warnings
                    t.assigned_team_id,
                    t.assigned_team != null ? t.assigned_team.team_name : "Unassigned",
                    t.answer ?? string.Empty
                ))
                .Take(50)
                .ToListAsync();

            // Ticket stats for the current requester
            var totalTickets = await _db.tickets
                .CountAsync(t => t.requester_id == uid.Value);

            var openTickets = await _db.tickets
                .CountAsync(t => t.requester_id == uid.Value && t.status == "OPEN");

            var inProgressTickets = await _db.tickets
                .CountAsync(t => t.requester_id == uid.Value && t.status == "INPROGRESS");

            var resolvedTickets = await _db.tickets
                .CountAsync(t => t.requester_id == uid.Value && t.status == "RESOLVED");

            var CanceledTickets = await _db.tickets
                .CountAsync(t => t.requester_id == uid.Value && t.status == "CANCELED");
            var AssignedTickets = await _db.tickets
                .CountAsync(t => t.requester_id == uid.Value && t.status == "ASSIGNED");

            // Store them for the Razor page
            ViewData["TotalTickets"] = totalTickets;
            ViewData["OpenTickets"] = openTickets;
            ViewData["InProgressTickets"] = inProgressTickets;
            ViewData["ResolvedTickets"] = resolvedTickets;
            ViewData["CanceledTickets"] = CanceledTickets;
            ViewData["AssignedTickets"] = AssignedTickets;

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
                type = "request",  // optional if your API supports it
                priority = Priority,
                status = "OPEN",
                requester_id = uid.Value,
                created_at = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
            };

            var success = await _apiClient.CreateTicketAsync(entity);

            if (success)
                TempData["ToastMessage"] = "Ticket created successfully via API!";
            else
                TempData["ToastMessage"] = "Failed to create ticket. Please try again.";

            // Refresh list
            return RedirectToPage();

        }
    }
}
