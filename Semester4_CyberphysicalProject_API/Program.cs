using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Semester4_CyberphysicalProject_API.Data;




//////////////////////////////////////////////////////////////////////////////////////
///                             Application Setup                                  ///
//////////////////////////////////////////////////////////////////////////////////////
///


// Create the web application builder, which loads configuration (appsettings.json)
// and sets up the dependency injection (DI) container.      
var builder = WebApplication.CreateBuilder(args);


// Register the Context Class in the DI container.
// Configures it to use SQL Server, with the connection string from appsettings.json.
// Throws a clear error at startup if the connection string is missing. 
builder.Services.AddDbContext<WeatherContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("WeatherContext") ?? throw new InvalidOperationException("Connection string 'WeatherContext' not found.")));

// Add services to the container.  
builder.Services.AddControllersWithViews();

// Build the application using all the services and configuration registered above.  
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // In production: redirect any unhandled errors to the /Home/Error page
    // instead of showing raw error details to the user.  
    app.UseExceptionHandler("/Home/Error");

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Redirect all HTTP requests to HTTPS for security.  
app.UseHttpsRedirection();

// Allow the app to serve static files (CSS, JS, images) from the wwwroot folder.  
app.UseStaticFiles();

// Enable routing — allows the app to match incoming URLs to the correct controller and action.  
app.UseRouting();

// Enable authorisation — checks if the user has permission to access a resource.
// Must come after UseRouting() and before MapControllerRoute().
app.UseAuthorization();





//////////////////////////////////////////////////////////////////////////////////////
///                                  Routing Paths                                 ///
//////////////////////////////////////////////////////////////////////////////////////
///


// Default route — catches all standard requests.
// Falls back to HomeController -> Index action if no controller/action is specified in the URL.  
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


// Route for the HelloWorld controller.
// Matches URLs like /HelloWorld or /HelloWorld/Index.    
app.MapControllerRoute(
    name: "helloWorld",
    pattern: "{controller=HelloWorld}/{action=Index}/{id?}");


// Route for the Weather controller.
// Matches URLs like /Weather or /Weather/Index.  
app.MapControllerRoute(
    name: "helloWorld",
    pattern: "{controller=Weather}/{action=Index}/{id?}");






/////////////////////////////////////////////////////////////////////////////////////////////
///                                  Database Setup Check                                 ///
/////////////////////////////////////////////////////////////////////////////////////////////
///


// Database initialisation:
// Before the app starts, ensure the SQL database and DbSchema exist on disk.
// If they don't exist yet, EF Core creates them automatically based on our model classes.
using (var scope = app.Services.CreateScope())
{
    // Ask the DI container to create a new Context Class Instance,
    // so we can perform a check, before we run our web application.
    var db = scope.ServiceProvider.GetRequiredService<WeatherContext>();

    // Check if the SQL database and DbSchema exist on disk.
    // If yes — nothing happens.
    // If no  — EF Core creates the SQL database and DbSchema automatically.
    db.Database.EnsureCreated();
}






////////////////////////////////////////////////////////////////////////////////////////////
///                                  Run Web Application                                 ///
////////////////////////////////////////////////////////////////////////////////////////////
///


// Runs the Application.  
app.Run();






