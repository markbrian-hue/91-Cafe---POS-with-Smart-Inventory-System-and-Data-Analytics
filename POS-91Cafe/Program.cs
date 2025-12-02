using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using POS_91Cafe.Data;
using System;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure the DbContext for MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var serverVersion = new MySqlServerVersion(new Version(8, 0, 29));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, serverVersion)
);

// 2. Add Authentication Services (NEW)
// This tells the app to track logins using Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "POS_91Cafe_OwnerSession";
        options.LoginPath = "/Auth/Login"; // If not logged in, go here automatically
        options.ExpireTimeSpan = TimeSpan.FromDays(7); // Stay logged in for 7 days
        options.SlidingExpiration = true;
    });

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 3. Enable Authentication & Authorization (ORDER IS CRITICAL)
app.UseAuthentication(); // <--- Checks "Who are you?" (Cookie)
app.UseAuthorization();  // <--- Checks "Are you allowed here?"

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Sales}/{action=Index}/{id?}");

app.Run();