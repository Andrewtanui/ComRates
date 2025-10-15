// Program.cs - Corrected version
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TanuiApp.Data;
using TanuiApp.Models;
using TanuiApp.Services;
using TanuiApp.Hubs;
using TanuiApp.Middleware;

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

    // Configure security stamp validation to check on every request
    builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    {
        // Validate security stamp on every request (0 seconds = immediate)
        // This ensures banned/suspended users are logged out immediately
        options.ValidationInterval = TimeSpan.Zero;
    });

    // App services
    builder.Services.AddScoped<IImageStorageService, ImageStorageService>();
    builder.Services.AddScoped<IChatbotService, EnhancedChatbotService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IOtpService, OtpService>();

    // SMTP Email
    builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

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

            // Seed default admin and chatbot training data
            var userManager = services.GetRequiredService<UserManager<Users>>();
            await DbSeeder.SeedDefaultAdmin(userManager);

            // Seed chatbot training data (idempotent) and retrain model
            var db = services.GetRequiredService<AppDbContext>();
            var chatbot = services.GetRequiredService<IChatbotService>();
            await DbSeeder.SeedChatbotTrainingData(db, chatbot);

            // Uncomment to seed test users for development
            // await DbSeeder.SeedTestData(userManager);
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
    
    // Ban check middleware - must come after authentication to check authenticated users
    app.UseBanCheck();

    // Configure authorization policies for different user roles
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure all required roles exist
        string[] roleNames = { "Buyer", "Seller", "DeliveryService", "SystemAdmin" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

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