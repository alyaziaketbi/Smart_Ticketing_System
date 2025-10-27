using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SmartTicketingManagementApp.Pages.Support
{
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
			var role = HttpContext.Session.GetString("u:role");
			if (role != "Support")
			{
				Response.Redirect("/Login");
			}
		}
    }
}
