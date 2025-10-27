using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SmartTicketingManagementApp.Data;
using SmartTicketingManagementApp.Data.Entities;
using SmartTicketingManagementApp.Models;
using System.Net.Sockets;

namespace SmartTicketingManagementApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        private readonly AppDbContext _db;

        public bool CanConnect { get; private set; }
        public List<ticket> Tickets { get; private set; } = new();
        public string? ErrorMessage { get; private set; }

        public IndexModel(ILogger<IndexModel> logger, AppDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task OnGetAsync()
        {

			// Check session — if user not logged in, go to Login
			var id = HttpContext.Session.GetInt32(SessionKeys.UserId);
			if (id == null)
			{
				Response.Redirect("/Login");
				return;
			}

			// If you want, you can display their name or role
			ViewData["Name"] = HttpContext.Session.GetString(SessionKeys.UserName);
			ViewData["Role"] = HttpContext.Session.GetString(SessionKeys.UserRole);

			//try
			//{
			//    CanConnect = await _db.Database.CanConnectAsync();

			//    if (CanConnect)
			//    {
			//        // Fetch top 20 rows (using AsNoTracking for lightweight read)
			//        Tickets = await _db.tickets
			//            .OrderByDescending(t => t.ticket_id)
			//            .Take(20)
			//            .AsNoTracking()
			//            .ToListAsync();
			//    }
			//}
			//catch (Exception ex)
			//{
			//    _logger.LogError(ex, "Error connecting to database");
			//    ErrorMessage = ex.Message;
			//    CanConnect = false;
			//}
		}
    }
}
