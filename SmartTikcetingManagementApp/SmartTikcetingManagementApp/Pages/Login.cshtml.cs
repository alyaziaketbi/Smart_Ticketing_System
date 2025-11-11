using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SmartTicketingManagementApp.Data;
using SmartTicketingManagementApp.Models;

namespace SmartTicketingManagementApp.Pages
{
    public class LoginModel : PageModel
    {

        private readonly AppDbContext _db;

        public LoginModel(AppDbContext db) => _db = db;

        [BindProperty] public string Email { get; set; } = "";
        public string? Error { get; set; }

        // Suggestion list
        public List<UserOption> Options { get; private set; } = new();

        public class UserOption
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public string? Email { get; set; }
        }

        public async Task OnGetAsync()
        {
            Options = await _db.users
                .AsNoTracking()
                .OrderBy(u => u.name)
                .Select(u => new UserOption { Id = u.user_id, Name = u.name, Email = u.email })
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                Error = "Email is required.";
                return Page();
            }

            // 1) find user by email
            var u = await _db.users.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.email == Email.Trim());
            if (u is null)
            {
                Error = "Invalid email.";
                return Page();
            }

			// 2) determine role: Agent if exists in team_member, else Requester
			var teamMember = await _db.team_members.AsNoTracking().FirstOrDefaultAsync(t => t.user_id == u.user_id);

			string role;
			if (teamMember == null)
				role = "Requester";
			else if (teamMember.team_id.Contains("HELP_DESK", StringComparison.OrdinalIgnoreCase))
				role = "HelpDesk";
			else
				role = "Support";


			// 3) store minimal info in Session
			HttpContext.Session.SetInt32(SessionKeys.UserId, u.user_id);
            HttpContext.Session.SetString(SessionKeys.UserName, u.name ?? "");
            HttpContext.Session.SetString(SessionKeys.UserEmail, u.email ?? "");
            HttpContext.Session.SetString(SessionKeys.UserRole, role);

            //  Store logged-in user info
            HttpContext.Session.SetInt32("UserId", u.user_id);
            HttpContext.Session.SetString("UserName", u.name ?? "");
            HttpContext.Session.SetString("UserEmail", u.email ?? "");
            HttpContext.Session.SetString("UserRole", role);

            // 4) go anywhere you like after login (e.g., Index)
            return role switch
			{
				"HelpDesk" => RedirectToPage("/HelpDesk/Index"),
				"Support" => RedirectToPage("/Support/Index"),
				_ => RedirectToPage("/Requester/Index")
			};
		}

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Login");
        }

        // Helper you can use elsewhere if you want to read the logged-in user
        public static CurrentUser? GetCurrent(HttpContext http)
        {
            var id = http.Session.GetInt32(SessionKeys.UserId);
            if (id is null) return null;
            return new CurrentUser
            {
                UserId = id.Value,
                Name = http.Session.GetString(SessionKeys.UserName) ?? "",
                Email = http.Session.GetString(SessionKeys.UserEmail) ?? "",
                Role = http.Session.GetString(SessionKeys.UserRole) ?? "Requester"
            };
        }
    }
}
