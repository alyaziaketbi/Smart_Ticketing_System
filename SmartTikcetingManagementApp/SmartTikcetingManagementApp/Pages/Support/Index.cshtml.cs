using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SmartTicketingManagementApp.Data;
using SmartTicketingManagementApp.Data.Entities;
using SmartTicketingManagementApp.Models;
using SmartTicketingManagementApp.Services;
using System.Text.Json;



namespace SmartTicketingManagementApp.Pages.Support
{
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {

        private readonly AppDbContext _db;
        private readonly ApiClient _apiClient;

        public IndexModel(AppDbContext db, ApiClient apiClient)
        {
            _db = db;
            _apiClient = apiClient;
        }
        public string? UserName { get; set; }
        public string? TeamName { get; set; }
        public string? TeamId { get; set; }
        public string? TeamDescription { get; set; }
        public int TotalAssigned { get; set; }
        public int NewTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public int CanceledTickets { get; set; }
        public ticket? SelectedTicket { get; private set; }

        [BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; } // optional if you plan to add filters later
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        private static readonly HashSet<string> AllowedStatuses =
            new(StringComparer.OrdinalIgnoreCase) { "NEW", "ASSIGNED", "INPROGRESS", "RESOLVED", "CANCELED" };


        public List<TicketRow> Items { get; set; } = new();
        public record TicketRow(
       int Id,
       string Subject,
       string Status,
       string? Priority,
       DateTime? CreatedAt,
       string Body,
       string? AssignedTeamId,
       string? AssignedTeamName,
       string? Answer,
       string UserName
   );
        public async Task<IActionResult> OnGetAsync()
        {
            var role = HttpContext.Session.GetString("u:role");
			if (role != "Support")
			{
				Response.Redirect("/Login");

            }
// Logged-in user id
                var uid = HttpContext.Session.GetInt32(SessionKeys.UserId);
                if (uid is null)
                    return RedirectToPage("/Login");

            // Get logged-in user info from session
            var current = LoginModel.GetCurrent(HttpContext);
            UserName = current?.Name ?? "Support Agent";

            // Get both team_id and team_name of the logged-in user
            var teamInfo = await (from tm in _db.team_members
                                  join t in _db.teams on tm.team_id equals t.team_id
                                  where tm.user_id == uid.Value
                                  select new { t.team_id, t.team_name })
                                  .FirstOrDefaultAsync();

            // Fallback if no team found
            if (string.IsNullOrEmpty(TeamName))
                TeamName = "Support";

            TeamName = teamInfo.team_name;
            TeamId = teamInfo.team_id;
            // Get team description
            TeamDescription = await _db.teams
                .Where(t => t.team_id == TeamId)
                .Select(t => t.team_description)
                .FirstOrDefaultAsync();

            // Ticket stats for this team
            TotalAssigned = await _db.tickets
                .CountAsync(t => t.assigned_team_id == TeamId);

            NewTickets = await _db.tickets
                .CountAsync(t => t.assigned_team_id == TeamId && t.status == "ASSIGNED");

            InProgressTickets = await _db.tickets
                .CountAsync(t => t.assigned_team_id == TeamId && t.status == "INPROGRESS");

            ResolvedTickets = await _db.tickets
                .CountAsync(t => t.assigned_team_id == TeamId && t.status == "RESOLVED");

            CanceledTickets = await _db.tickets
                .CountAsync(t => t.assigned_team_id == TeamId && t.status == "CANCELED");


            // Base query: only tickets assigned to this team
            IQueryable<ticket> q = _db.tickets
                .Include(t => t.assigned_team)
                .Include(t => t.requester)
                .Where(t => t.assigned_team_id == TeamId);


            /// Apply status filter
            if (!string.IsNullOrWhiteSpace(Status) && Status != "All")
            {
                if (!AllowedStatuses.Contains(Status.ToUpper()))
                {
                    Items = new();
                    TotalCount = 0;
                    return Page();
                }

                q = q.Where(t => t.status.ToUpper() == Status.ToUpper());
            }

            // Order and limit
            q = q.OrderByDescending(t => t.created_at);

            // Count and paginate directly in database
            TotalCount = await q.Take(50).CountAsync();
            var tickets = await q.Take(50)
                .Skip((Page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Now project to TicketRow in memory
            Items = tickets.Select(t => new TicketRow(
                t.ticket_id,
                t.subject ?? string.Empty,
                t.status ?? string.Empty,
                t.priority,
                t.created_at,
                t.body ?? string.Empty,
                t.assigned_team_id,
                t.assigned_team?.team_name ?? "Unassigned",
                t.answer ?? string.Empty,
                t.requester?.name ?? "—"
            )).ToList();

            // If a ticket is selected for viewing
            var viewId = Request.Query["viewId"].FirstOrDefault();
            if (int.TryParse(viewId, out int tid))
            {
                SelectedTicket = await _db.tickets
                    .Include(t => t.requester)
                    .Include(t => t.assigned_team)
                    .FirstOrDefaultAsync(t => t.ticket_id == tid && t.assigned_team_id == TeamId);
            }


            return Page();
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostStartWorkAsync([FromBody] JsonElement data)
        {
            if (!data.TryGetProperty("ticketId", out var idProp))
                return BadRequest();

            var ticketId = idProp.GetInt32();
            var ticket = await _db.tickets.FirstOrDefaultAsync(t => t.ticket_id == ticketId);
            if (ticket == null)
                return NotFound();

            ticket.status = "INPROGRESS";
            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostSuggestSolutionAsync([FromBody] JsonElement data)
        {
            try
            {
                if (!data.TryGetProperty("ticketId", out var idProp))
                    return new JsonResult(new { success = false, message = "Missing ticketId" });

                int ticketId = idProp.GetInt32();
                var response = await _apiClient.SuggestSolutionAsync(ticketId);

                if (response == null)
                    return new JsonResult(new { success = false, message = "No response from API." });

                var solutionText = response.solution ??
                                   (response.sources?.FirstOrDefault()?.title ?? "No solution found.");

                return new JsonResult(new { success = true, solution = solutionText });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostUpdateTicketAsync([FromBody] TicketUpdateModel update)
        {
            if (update == null || update.TicketId <= 0)
                return new JsonResult(new { success = false, message = "Invalid request." });

            var ticket = await _db.tickets.FindAsync(update.TicketId);
            if (ticket == null)
                return new JsonResult(new { success = false, message = "Ticket not found." });

            if (!string.IsNullOrWhiteSpace(update.NewStatus))
                ticket.status = update.NewStatus.ToUpper();

            if (!string.IsNullOrWhiteSpace(update.Answer))
                ticket.answer = update.Answer;

            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "Ticket updated successfully." });
        }

        public class TicketUpdateModel
        {
            public int TicketId { get; set; }
            public string? NewStatus { get; set; }
            public string? Answer { get; set; }
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostFindSimilarAsync([FromBody] JsonElement data)
        {
            if (!data.TryGetProperty("ticketId", out var idProp))
                return new JsonResult(new { success = false, message = "Missing ticketId" });

            int ticketId = idProp.GetInt32();
            var response = await _apiClient.FindSimilarAsync(ticketId);

            if (response?.Results == null || response.Results.Count == 0)
                return new JsonResult(new { success = false, message = "No similar tickets found." });

            var topResults = response.Results.Take(5).Select(r => new
            {
                ticket_id = r.Ticket_Id,
                title = r.Title,
                answer = r.Answer,
                assigned_team_name = r.Assigned_Team_Name,
               // description = r.Description // shamma will add this later
            });

            return new JsonResult(new { success = true, results = topResults });
        }


    }
}
