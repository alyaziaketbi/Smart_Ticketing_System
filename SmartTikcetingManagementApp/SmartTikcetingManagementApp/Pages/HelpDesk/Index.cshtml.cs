using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SmartTicketingManagementApp.Data;
using SmartTicketingManagementApp.Data.Entities;
using SmartTicketingManagementApp.Services;
using static SmartTicketingManagementApp.Pages.HelpDesk.IndexModel;

namespace SmartTicketingManagementApp.Pages.HelpDesk
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

        // bound so the page can re-render the selected value
        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10; // default “top 10 latest” per your page
        public IReadOnlyList<helpdesk_ticket> Tickets { get; private set; } = Array.Empty<helpdesk_ticket>();

        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase) { "Open", "Assigned", "Resolved", "Canceled" };
        public int TotalCount { get; private set; }

        public List<team> Teams { get; private set; } = new();
        public helpdesk_ticket? SelectedTicket { get; private set; }

        public string? SuggestedTeam { get; private set; }
        public string? SuggestionReason { get; private set; }
        public double? Confidence { get; private set; }
        public string? UserName { get; set; }

        public record TeamSummary(
         string TeamName,
         string TeamDescription,
         int TotalTickets,
         int ActiveTickets
             );
        public List<TeamSummary> TeamStats { get; private set; } = new();

        public string? SelectedTeam { get; set; }


        public async Task<IActionResult> OnGetAsync(int? viewId)
        {
            // must be Help Desk user
			var role = HttpContext.Session.GetString("u:role");
			if (role != "HelpDesk")
			{
				Response.Redirect("/Login");
			}

            if (Page < 1) Page = 1;
            if (PageSize < 1 || PageSize > 100) PageSize = 10;

            IQueryable<helpdesk_ticket> q = _db.helpdesk_tickets.AsNoTracking();


            // Get user info from session
            var current = LoginModel.GetCurrent(HttpContext);
            UserName = current?.Name ?? "User";


            // Filter by status if provided (matches the string you set in the view)
            if (!string.IsNullOrWhiteSpace(Status))
            {
                if (!AllowedStatuses.Contains(Status))
                {
                    // unknown status – return empty list
                    Tickets = Array.Empty<helpdesk_ticket>();
                    TotalCount = 0;
                    return Page();
                }
                q = q.Where(t => t.status == Status);
            }

            q = q.OrderByDescending(t => t.created_at);

            TotalCount = await q.CountAsync();
            Tickets = await q.Skip((Page - 1) * PageSize)
                             .Take(PageSize)
                             .ToListAsync();
            
            // Load teams for dropdown
            Teams = await _db.teams.AsNoTracking().ToListAsync();

            // If a viewId is provided, get the details for that ticket
            if (viewId.HasValue)
            {
                SelectedTicket = await _db.helpdesk_tickets
                    .FirstOrDefaultAsync(t => t.ticket_id == viewId);
            }

            // ?? Dashboard Stats (Requester style)
            var totalTickets = await _db.tickets.CountAsync();

            var unassignedTickets = await _db.tickets
                .CountAsync(t => t.assigned_team_id == null);

            var inProgressTickets = await _db.tickets
                .CountAsync(t => t.status == "INPROGRESS" || t.status == "ASSIGNED");

            var resolvedTickets = await _db.tickets
                .CountAsync(t => t.status == "RESOLVED");
            var CanceledTickets = await _db.tickets
                .CountAsync(t => t.status == "CANCELED");

            ViewData["TotalTickets"] = totalTickets;
            ViewData["UnassignedTickets"] = unassignedTickets;
            ViewData["InProgressTickets"] = inProgressTickets;
            ViewData["ResolvedTickets"] = resolvedTickets;
            ViewData["CanceledTickets"] = CanceledTickets;

            TeamStats = await _db.teams
    .Select(team => new TeamSummary(
        team.team_name,
        team.team_description ?? "No description available",
        _db.tickets.Count(t => t.assigned_team_id == team.team_id),
        _db.tickets.Count(t => t.assigned_team_id == team.team_id &&
                               (t.status == "ASSIGNED" || t.status == "INPROGRESS"))
    ))
    .ToListAsync();



            return Page();
        }

        public async Task<IActionResult> OnPostSuggestAsync(int ticketId)
        {
            var response = await _apiClient.AssignTicketAsync(ticketId);

            if (response != null)
            {
                SuggestedTeam = response.assigned_team_name;
                SuggestionReason = response.reasoning;
            }

            await LoadPageDataAsync(ticketId); // ? keep table visible
            //SelectedTicket = await _db.helpdesk_tickets
              //  .FirstOrDefaultAsync(t => t.ticket_id == ticketId);


            return Page();
        }

        public async Task<IActionResult> OnPostAssignAsync(int ticketId, string teamName)
        {
            Console.WriteLine($"Suggest API called for ticket: {ticketId}");

            // Find the team by name to get its ID
            var team = await _db.teams.FirstOrDefaultAsync(t => t.team_name == teamName);
            if (team == null)
            {
                TempData["Message"] = "Selected team not found.";
                await LoadPageDataAsync(ticketId);
                return Page();
            }

            // Find the ticket and update its assigned_team_id
            var ticket = await _db.tickets.FindAsync(ticketId);
            if (ticket != null)
            {
                ticket.assigned_team_id = team.team_id;
                ticket.status = "ASSIGNED";
                await _db.SaveChangesAsync();
            }
            await LoadPageDataAsync(ticketId); // ? keep table visible
            SelectedTicket = await _db.helpdesk_tickets
        .FirstOrDefaultAsync(t => t.ticket_id == ticketId);

            TempData["Message"] = $"Ticket assigned to {team.team_name}.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAcceptAsync(string teamName, int ticketId)
        {
            // Store the accepted suggestion so it appears selected in the dropdown
            SelectedTeam = teamName;

            // Keep blue box visible
            var response = await _apiClient.AssignTicketAsync(ticketId);
            if (response != null)
            {
                SuggestionReason = response.reasoning;
            }

            await LoadPageDataAsync(ticketId); // ? keep table visible
          //  SelectedTicket = await _db.helpdesk_tickets
        //.FirstOrDefaultAsync(t => t.ticket_id == ticketId);


            // Just reload the page (the modal will remain visible with the dropdown updated)
            return Page();
        }

        private async Task LoadPageDataAsync(int? ticketId = null)
        {
            // Reload tickets
            Tickets = await _db.helpdesk_tickets
                .AsNoTracking()
                .OrderByDescending(t => t.created_at)
                .ToListAsync();

            // Reload teams
            Teams = await _db.teams.AsNoTracking().ToListAsync();

            /// ? Dashboard stats (moved here from OnGetAsync)
            var totalTickets = await _db.tickets.CountAsync();
            var unassignedTickets = await _db.tickets
                .CountAsync(t => t.assigned_team_id == null);
            var inProgressTickets = await _db.tickets
                .CountAsync(t => t.status == "INPROGRESS" || t.status == "ASSIGNED");
            var resolvedTickets = await _db.tickets
                .CountAsync(t => t.status == "RESOLVED");

            ViewData["TotalTickets"] = totalTickets;
            ViewData["UnassignedTickets"] = unassignedTickets;
            ViewData["InProgressTickets"] = inProgressTickets;
            ViewData["ResolvedTickets"] = resolvedTickets;


            // ? Support team stats (if shown in UI)
            var teamWorkload = await _db.teams
        .Select(t => new
        {
            t.team_name,
            TicketCount = _db.tickets.Count(x => x.assigned_team_id == t.team_id)
        })
        .ToListAsync();


            ViewData["SupportTeams"] = teamWorkload;

            // ? If a specific ticket is open in the modal, reload it too
            if (ticketId.HasValue)
            {
                SelectedTicket = await _db.helpdesk_tickets
                    .FirstOrDefaultAsync(t => t.ticket_id == ticketId.Value);
            }
        }

        public async Task<IActionResult> OnPostDismissAsync(int ticketId)
        {
            SuggestedTeam = null;
            SuggestionReason = null;

            await LoadPageDataAsync(ticketId);
            return Page();
        }



    }
}
