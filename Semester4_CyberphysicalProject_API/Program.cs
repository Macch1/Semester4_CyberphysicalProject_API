using Microsoft.EntityFrameworkCore;
using Semester4_CyberphysicalProject_API.Data;
using Semester4_CyberphysicalProject_API.Services;
using Semester4_CyberphysicalProject_API.Services.Interfaces;




//////////////////////////////////////////////////////////////////////////////////////
///                             Application Setup                                  ///
//////////////////////////////////////////////////////////////////////////////////////


// Create the web application builder, which loads configuration (appsettings.json)
// and sets up the dependency injection (DI) container.
var builder = WebApplication.CreateBuilder(args);


// Register the Context Class Factory in the DI container.
// Uses IDbContextFactory instead of AddDbContext, allowing WeatherFacade to be
// registered as a singleton - the factory creates and disposes a fresh Context
// Class Instance per database operation, avoiding lifetime conflicts.
builder.Services.AddDbContextFactory<WeatherContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("WeatherContext")
        ?? throw new InvalidOperationException("Connection string 'WeatherContext' not found.")));


// Register the service interfaces and their implementations in the DI container.
// HttpClient is registered for DMI_Ocean_Service and StationConfig_Service
// so they can make HTTP calls to the DMI API.

// Register IDMI_Ocean_Service -> DMI_Ocean_Service.
// Singleton - HttpClient is thread-safe and designed to be reused.
builder.Services.AddHttpClient<IDMI_Ocean_Service, DMI_Ocean_Service>();

// Register IStationConfig_Service -> StationConfig_Service.
// Singleton - only reads and writes to a local JSON file, no lifetime conflicts.
builder.Services.AddHttpClient<IStationConfig_Service, StationConfig_Service>();

// Register WeatherFacade as a singleton, implementing both facade interfaces.
// Safe as a singleton because it uses IDbContextFactory for database access,
// creating and disposing a fresh Context Class Instance per operation.
builder.Services.AddSingleton<WeatherFacade>();
builder.Services.AddSingleton<IWeatherDisplay_Facade>(p => p.GetRequiredService<WeatherFacade>());
builder.Services.AddSingleton<IWeatherFetch_Facade>(p => p.GetRequiredService<WeatherFacade>());

// Register DmiFetch_BackgroundService as a hosted background service.
// ASP.NET Core starts it automatically when app.RunAsync() is called,
// and stops it gracefully when the application shuts down.
builder.Services.AddHostedService<DmiFetch_BackgroundService>();

// Add services to the container.
builder.Services.AddControllersWithViews();


// Build the application using all the services and configuration registered above.
var app = builder.Build();




//////////////////////////////////////////////////////////////////////////////////////
///                             HTTP Request Pipeline                              ///
//////////////////////////////////////////////////////////////////////////////////////


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // In production: redirect any unhandled errors to the /Home/Error page
    // instead of showing raw error details to the user.
    app.UseExceptionHandler("/Home/Error");

    // The default HSTS value is 30 days. You may want to change this for
    // production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Redirect all HTTP requests to HTTPS for security.
app.UseHttpsRedirection();

// Allow the app to serve static files (CSS, JS, images) from the wwwroot folder.
app.UseStaticFiles();

// Enable routing - allows the app to match incoming URLs to the correct controller and action.
app.UseRouting();

// Enable authorisation - checks if the user has permission to access a resource.
// Must come after UseRouting() and before MapControllerRoute().
app.UseAuthorization();




//////////////////////////////////////////////////////////////////////////////////////
///                                  Routing Paths                                 ///
//////////////////////////////////////////////////////////////////////////////////////


// Default route - catches all standard requests.
// Falls back to WeatherController -> Index action if no controller/action is specified.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Weather}/{action=Index}/{id?}");

// Route for the Home controller.
// Matches URLs like /Home or /Home/Index.
app.MapControllerRoute(
    name: "home",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Route for the Weather controller.
// Matches URLs like /Weather or /Weather/Index.
app.MapControllerRoute(
    name: "weather",
    pattern: "{controller=Weather}/{action=Index}/{id?}");




/////////////////////////////////////////////////////////////////////////////////////////////
///                                  Database Setup Check                                 ///
/////////////////////////////////////////////////////////////////////////////////////////////


// Database initialisation:
// Before the app starts, ensure the SQL database and DbSchema exist on disk.
// If they don't exist yet, EF Core creates them automatically based on our model classes.
using (IServiceScope scope = app.Services.CreateScope())
{
    // Ask the DI container to create a new Context Class Instance,
    // so we can perform a check before we run our web application.
    // We use IDbContextFactory here since we switched to factory-based registration.
    IDbContextFactory<WeatherContext> contextFactory = scope.ServiceProvider
        .GetRequiredService<IDbContextFactory<WeatherContext>>();

    // Create a fresh Context Class Instance from the factory.
    using WeatherContext db = contextFactory.CreateDbContext();

    // Check if the SQL database and DbSchema exist on disk.
    // If yes - nothing happens.
    // If no  - EF Core creates the SQL database and DbSchema automatically.
    db.Database.EnsureCreated();

    // Check if a full reset has been requested in appsettings.json.
    // If Refresh_Setup is true, wipe all readings and delete the station list file,
    // forcing a fresh station discovery on the next fetch cycle.
    // NOTE: You must manually set Refresh_Setup back to false after a reset.
    // Leaving it as true will wipe all data on every restart - losing all history.
    bool refreshSetup = app.Configuration.GetValue<bool>("Refresh_Setup", false);
    if (refreshSetup)
    {
        Console.WriteLine("[INFO] Program.cs: Refresh_Setup is true. Wiping all data and resetting stations.");

        // Delete all readings from the SQL database.
        db.Readings.RemoveRange(db.Readings);
        await db.SaveChangesAsync();
        Console.WriteLine("[INFO] Program.cs: All readings deleted from the SQL database.");

        // Delete the station list file, forcing fresh discovery on next cycle.
        IStationConfig_Service stationConfig = scope.ServiceProvider
            .GetRequiredService<IStationConfig_Service>();
        stationConfig.DeleteStationFile();
        Console.WriteLine("[INFO] Program.cs: Station list file deleted. Discovery will run on next cycle.");
        Console.WriteLine("[WARNING] Program.cs: Remember to set Refresh_Setup back to false in appsettings.json!");
    }
}




////////////////////////////////////////////////////////////////////////////////////////////
///                                  Run Web Application                                 ///
////////////////////////////////////////////////////////////////////////////////////////////


// Runs the Application.
// Uses RunAsync() instead of Run() to support async operations above,
// specifically the await db.SaveChangesAsync() call in the reset block.
await app.RunAsync();