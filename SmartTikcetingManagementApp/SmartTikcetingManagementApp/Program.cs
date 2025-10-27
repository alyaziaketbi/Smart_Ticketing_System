using Microsoft.EntityFrameworkCore;
using SmartTicketingManagementApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("SmartTicketsDb")));

builder.Services.AddHttpContextAccessor();

// Session (no cookies/auth frameworks)
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.MapControllers();
app.MapRazorPages();
app.UseAuthorization();
app.MapRazorPages();

app.MapGet("/", context =>
{
	var role = context.Session.GetString("u:role");
	var dest = role switch
	{
		"HelpDesk" => "/HelpDesk/Index",
		"Support" => "/Support/Index",
		"Requester" => "/Requester/Index",
		_ => "/Login"
	};

	context.Response.Redirect(dest);
	return Task.CompletedTask;
});

app.Run();
