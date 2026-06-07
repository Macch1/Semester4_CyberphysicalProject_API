using Microsoft.AspNetCore.Mvc;
using Semester4_CyberphysicalProject_API.DMI_Objects;
using Semester4_CyberphysicalProject_API.Models;
using Semester4_CyberphysicalProject_API.Services.Interfaces;

namespace Semester4_CyberphysicalProject_API.Controllers
{
    /// <summary>
    /// The main controller for the weather display page.
    /// Responsible for reading and displaying sea water temperature data.
    /// Only ever talks to IWeatherDisplay_Facade and IWeatherFetch_Facade -
    /// never directly to the database or DMI services.
    /// </summary>
    public class WeatherController : Controller
    {

        // Time range constants - number of hours to fetch for each tab.
        private const int HOURS_24 = 24;
        private const int HOURS_7_DAYS = 168;
        private const string RANGE_24H = "24h";
        private const string RANGE_7D = "7d";

        // Configuration key constants.
        private const string CONFIG_FETCH_INTERVAL_MINUTES = "Dmi:FetchIntervalMinutes";
        private const string CONFIG_TOO_COLD_BELOW = "Dmi:SwimmingThresholds:TooColdBelow";
        private const string CONFIG_BRAVE_SWIMMERS_BELOW = "Dmi:SwimmingThresholds:BraveSwimmersBelow";

        // Default configuration values, used if the key is not found in appsettings.json.
        private const int DEFAULT_FETCH_INTERVAL_MINUTES = 15;
        private const double DEFAULT_TOO_COLD_BELOW = 17.0;
        private const double DEFAULT_BRAVE_SWIMMERS_BELOW = 20.0;




        // Read Only

        /// <summary>
        /// The display facade, used for reading and displaying data.
        /// Given as a parameter (DI).
        /// </summary>
        private readonly IWeatherDisplay_Facade _displayFacade;

        /// <summary>
        /// The fetch facade, used for triggering a manual fetch cycle.
        /// Given as a parameter (DI).
        /// </summary>
        private readonly IWeatherFetch_Facade _fetchFacade;

        /// <summary>
        /// Provides access to the appsettings.json configuration values.
        /// Given as a parameter (DI).
        /// </summary>
        private readonly IConfiguration _config;




        /////////////////////////////////////////////////////////////////////////////////
        ///                             Constructors                                  ///
        /////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Creates a new WeatherController instance.
        /// All dependencies are given as parameters (DI).
        /// </summary>
        /// <param name="displayFacade">The display facade, given as a parameter (DI).</param>
        /// <param name="fetchFacade">The fetch facade, given as a parameter (DI).</param>
        /// <param name="config">Provides access to appsettings.json values, given as a parameter (DI).</param>
        public WeatherController(IWeatherDisplay_Facade displayFacade, IWeatherFetch_Facade fetchFacade, IConfiguration config)
        {
            this._displayFacade = displayFacade;
            this._fetchFacade = fetchFacade;
            this._config = config;
        }




        ///////////////////////////////////////////////////////////////////////////////////
        ///                          Public Display Actions                            ///
        ///////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// The main page action.
        /// Fetches readings, warmest reading, swimming verdict, tracked stations,
        /// and the latest observed timestamp from the display facade and passes
        /// them to the view via ViewBag.
        /// Supports showing either the last 24 hours or the last 7 days of data,
        /// controlled by the "range" query parameter.
        /// </summary>
        /// <param name="range">
        /// The time range to display. Accepts "24h" for the last 24 hours (default)
        /// or "7d" for the last 7 days.
        /// </param>
        public async Task<IActionResult> Index(string range = RANGE_24H)
        {
            // Determine the number of hours to fetch based on the range parameter.
            int hours = range == RANGE_7D ? HOURS_7_DAYS : HOURS_24;

            // Fetch all data needed by the view from the display facade.
            List<WaterTemperatureReading> readings = await this._displayFacade.GetReadingsAsync(hours);
            WaterTemperatureReading? warmestReading = await this._displayFacade.GetWarmestReadingAsync();
            List<DMI_Station_DataClass> trackedStations = this._displayFacade.GetTrackedStations();
            DateTime? latestObservedAt = await this._displayFacade.GetLatestObservedAtAsync();

            // Calculate the swimming verdict for the warmest reading if one exists.
            // Nullable because there may be no readings yet on first run.
            SwimmingVerdict_Enum? verdict = warmestReading != null
                ? this._displayFacade.GetSwimmingVerdict(warmestReading.WaterTemperature)
                : null;

            // Read configuration values needed by the view.
            double tooColdBelow = this._config.GetValue<double>(CONFIG_TOO_COLD_BELOW, DEFAULT_TOO_COLD_BELOW);
            double braveBelow = this._config.GetValue<double>(CONFIG_BRAVE_SWIMMERS_BELOW, DEFAULT_BRAVE_SWIMMERS_BELOW);
            int intervalMinutes = this._config.GetValue<int>(CONFIG_FETCH_INTERVAL_MINUTES, DEFAULT_FETCH_INTERVAL_MINUTES);

            // Pass all data to the view via ViewBag.
            ViewBag.Readings = readings;
            ViewBag.WarmestReading = warmestReading;
            ViewBag.TrackedStations = trackedStations;
            ViewBag.Verdict = verdict;
            ViewBag.Range = range;
            ViewBag.Hours = hours;
            ViewBag.LatestObservedAt = latestObservedAt;
            ViewBag.TooColdBelow = tooColdBelow;
            ViewBag.BraveBelow = braveBelow;
            ViewBag.FetchIntervalMinutes = intervalMinutes;

            return View();
        }


        /// <summary>
        /// Returns the most recent ObservedAt timestamp as a UTC ISO-8601 JSON string.
        /// Called by the JavaScript poller in the view to detect when new data has arrived.
        /// Returns null JSON if no readings exist yet.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> LatestTimestamp()
        {
            DateTime? latest = await this._displayFacade.GetLatestObservedAtAsync();

            if (!latest.HasValue)
            {
                return Json(null);
            }

            // Return as UTC ISO-8601 with Z suffix so JavaScript parses it correctly.
            // Without the Z suffix, Danish locale machines replace colons with dots,
            // causing Invalid Date errors in the browser.
            return Json(DateTime.SpecifyKind(latest.Value, DateTimeKind.Utc).ToString("o"));
        }




        ///////////////////////////////////////////////////////////////////////////////////
        ///                          Public Fetch Actions                              ///
        ///////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Manual refresh action.
        /// Checks if enough time has passed since the last fetch before triggering a new one.
        /// If the fetch interval has not passed yet, skips the fetch and redirects back immediately.
        /// This prevents inconsistent data from appearing if the user refreshes too soon.
        /// Called when the user clicks the manual refresh button on the page.
        /// </summary>
        /// <param name="range">
        /// The current time range - preserved across the refresh so the user
        /// stays on the same view they were on before clicking refresh.
        /// </param>
        public async Task<IActionResult> Refresh(string range = RANGE_24H)
        {
            // Read the fetch interval from appsettings.json.
            int intervalMinutes = this._config.GetValue<int>(CONFIG_FETCH_INTERVAL_MINUTES, DEFAULT_FETCH_INTERVAL_MINUTES);

            // Get the most recent ObservedAt timestamp from the database.
            DateTime? latestObservedAt = await this._displayFacade.GetLatestObservedAtAsync();

            // If readings exist, check if enough time has passed since the last fetch.
            if (latestObservedAt != null)
            {
                double minutesSinceLastFetch = (DateTime.UtcNow - latestObservedAt.Value).TotalMinutes;

                if (minutesSinceLastFetch < intervalMinutes)
                {
                    // Not enough time has passed - skip the fetch and redirect back.
                    Console.WriteLine($"[INFO] WeatherController.Refresh: Skipping fetch - only {minutesSinceLastFetch:F1} minutes since last fetch. Interval is {intervalMinutes} minutes.");
                    return RedirectToAction(nameof(Index), new { range });
                }
            }

            // Enough time has passed - trigger an immediate fetch cycle.
            await this._fetchFacade.FetchAndSaveLatestAsync();

            // Redirect back to the main page, preserving the current range.
            return RedirectToAction(nameof(Index), new { range });
        }
    }
}