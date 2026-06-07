using Semester4_CyberphysicalProject_API.Services.Interfaces;

namespace Semester4_CyberphysicalProject_API.Services
{
    /// <summary>
    /// A hosted background service that runs continuously while the application is running.
    /// Responsible for waking up on a configured interval and triggering a fetch cycle
    /// via IWeatherFetch_Facade.
    /// 
    /// Uses IServiceScopeFactory to create a fresh scope for each fetch cycle.
    /// This is necessary because IWeatherFetch_Facade depends on WeatherContext,
    /// which is a scoped service — meaning it must not be held permanently by a singleton.
    /// Creating a fresh scope per cycle ensures WeatherContext is created and disposed cleanly.
    /// </summary>
    public class DmiFetch_BackgroundService : BackgroundService
    {

        // Configuration key constants.
        private const string CONFIG_FETCH_INTERVAL_MINUTES = "Dmi:FetchIntervalMinutes";

        // Default fetch interval in minutes, used if the key is not found in appsettings.json.
        private const int DEFAULT_FETCH_INTERVAL_MINUTES = 15;




        // Read Only

        /// <summary>
        /// The fetch facade, used for fetching and storing data.
        /// Given as a parameter (DI) — safe to hold permanently since WeatherFacade
        /// is registered as a singleton and uses IDbContextFactory internally.
        /// </summary>
        private readonly IWeatherFetch_Facade _fetchFacade;

        /// <summary>
        /// Provides access to the appsettings.json configuration values.
        /// </summary>
        private readonly IConfiguration _config;











        /////////////////////////////////////////////////////////////////////////////////
        ///                             Constructors                                  ///
        /////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Creates a new DmiFetch_BackgroundService instance.
        /// Both dependencies are given as parameters (DI).
        /// </summary>
        /// <param name="fetchFacade">The fetch facade, given as a parameter (DI).</param>
        /// <param name="config">Provides access to appsettings.json values, given as a parameter (DI).</param>
        public DmiFetch_BackgroundService(IWeatherFetch_Facade fetchFacade, IConfiguration config)
        {
            this._fetchFacade = fetchFacade;
            this._config = config;
        }












        ///////////////////////////////////////////////////////////////////////////////////
        ///                         Protected Execution Methods                        ///
        ///////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// The main execution loop of the background service.
        /// Called automatically by ASP.NET Core when the application starts.
        /// Runs until the application shuts down or the cancellation token is triggered.
        /// 
        /// On first run, checks if stations are ready — if not, initialises them first.
        /// Then enters a loop, checking the fetch interval and fetching data when ready.
        /// </summary>
        /// <param name="stoppingToken">
        /// A cancellation token that is triggered when the application is shutting down.
        /// The loop checks this token on every iteration to exit cleanly.
        /// </param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("[INFO] DmiFetch_BackgroundService: Background service started.");

            // Run the fetch loop until the application shuts down.
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Check if stations have been discovered yet.
                    // If not, run the discovery process before attempting a fetch.
                    if (!this._fetchFacade.StationsReady())
                    {
                        Console.WriteLine("[INFO] DmiFetch_BackgroundService: Stations not ready. Running station discovery.");
                        await this._fetchFacade.InitialiseStationsAsync();
                        Console.WriteLine("[INFO] DmiFetch_BackgroundService: Station discovery complete.");
                    }

                    // Read the fetch interval from appsettings.json.
                    // Defaults to 15 minutes if not configured.
                    int intervalMinutes = this._config.GetValue<int>(CONFIG_FETCH_INTERVAL_MINUTES, DEFAULT_FETCH_INTERVAL_MINUTES);

                    // Check the most recent ObservedAt timestamp in the database.
                    // This ensures we respect the fetch interval regardless of whether
                    // the last fetch was triggered by the background service or the Controller.
                    DateTime? lastFetchTimestamp = await this._fetchFacade.GetLastFetchTimestampAsync();

                    if (lastFetchTimestamp != null)
                    {
                        // Calculate how many minutes have passed since the last fetch.
                        double minutesSinceLastFetch = (DateTime.UtcNow - lastFetchTimestamp.Value).TotalMinutes;

                        if (minutesSinceLastFetch < intervalMinutes)
                        {
                            // Not enough time has passed — skip this cycle.
                            Console.WriteLine($"[INFO] DmiFetch_BackgroundService: Skipping fetch — only {minutesSinceLastFetch:F1} minutes since last fetch. Interval is {intervalMinutes} minutes.");
                        }
                        else
                        {
                            // Enough time has passed — run the full fetch cycle.
                            Console.WriteLine("[INFO] DmiFetch_BackgroundService: Starting fetch cycle.");
                            await this._fetchFacade.FetchAndSaveLatestAsync();
                            Console.WriteLine("[INFO] DmiFetch_BackgroundService: Fetch cycle complete.");
                        }
                    }
                    else
                    {
                        // No readings exist yet — fetch immediately.
                        Console.WriteLine("[INFO] DmiFetch_BackgroundService: No readings in database yet. Starting first fetch cycle.");
                        await this._fetchFacade.FetchAndSaveLatestAsync();
                        Console.WriteLine("[INFO] DmiFetch_BackgroundService: First fetch cycle complete.");
                    }
                }
                catch (Exception ex)
                {
                    // Log the error and continue — a single failing cycle must not stop the service.
                    // The next cycle will be attempted after the configured interval.
                    Console.WriteLine($"[ERROR] DmiFetch_BackgroundService: Fetch cycle failed. {ex.Message}");
                }

                // Read the fetch interval from appsettings.json.
                // Defaults to 15 minutes if not configured.
                int delayMinutes = this._config.GetValue<int>(CONFIG_FETCH_INTERVAL_MINUTES, DEFAULT_FETCH_INTERVAL_MINUTES);

                // Wait for the configured interval before the next cycle.
                // The delay respects the cancellation token — if the app shuts down during the wait,
                // the delay exits cleanly without throwing an unhandled exception.
                await Task.Delay(TimeSpan.FromMinutes(delayMinutes), stoppingToken);
            }

            Console.WriteLine("[INFO] DmiFetch_BackgroundService: Background service stopped.");
        }





    }
}