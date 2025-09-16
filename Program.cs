// Program.cs - Corrected version
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TanuiApp.Data;
using TanuiApp.Models;
using TanuiApp.Services;
using TanuiApp.Hubs;

var builder = WebApplication.CreateBuilder(args);

try
{
    // Add services to the container.
    builder.Services.AddControllersWithViews();
    builder.Services.AddSignalR();

    // Configure Entity Framework with proper error handling
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        options.UseSqlServer(connectionString);

        // Only enable detailed logging in development
        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
            options.LogTo(Console.WriteLine, LogLevel.Information);
        }
    });

    // Configure Identity
    builder.Services.AddIdentity<Users, IdentityRole>(options =>
    {
        // Password settings
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;

        // User settings
        options.User.RequireUniqueEmail = true;

        // Sign-in settings
        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedPhoneNumber = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    // App services
    builder.Services.AddScoped<IImageStorageService, ImageStorageService>();

    // Configure logging
    builder.Services.AddLogging(logging =>
    {
        if (builder.Environment.IsDevelopment())
        {
            logging.AddConsole();
            logging.AddDebug();
            logging.SetMinimumLevel(LogLevel.Information);
        }
        else
        {
            logging.SetMinimumLevel(LogLevel.Warning);
        }
    });

    var app = builder.Build();

    // Database initialization with proper error handling
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            logger.LogInformation("Testing database connection...");

            // Test database connection
            await context.Database.CanConnectAsync();
            logger.LogInformation("Database connection successful");

            // Ensure database exists
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Database initialization completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database");
            throw new InvalidOperationException("Database initialization failed. Please check your connection string and ensure SQL Server is running.", ex);
        }
    }

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    // Global exception handler middleware (should be early in pipeline)
    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Unhandled exception in request pipeline: {RequestPath}", context.Request.Path);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "text/plain";

                if (app.Environment.IsDevelopment())
                {
                    await context.Response.WriteAsync($"Error: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}");
                }
                else
                {
                    await context.Response.WriteAsync("An internal server error occurred.");
                }
            }
        }
    });

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    // Authentication middleware MUST come before authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Map routes
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapHub<MessageHub>("/hubs/messages");
    app.MapHub<NotificationHub>("/hubs/notifications");

    Console.WriteLine("Starting TanuiApp...");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Application failed to start: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    throw;
}