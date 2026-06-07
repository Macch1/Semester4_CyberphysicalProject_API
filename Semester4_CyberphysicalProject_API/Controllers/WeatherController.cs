using Microsoft.AspNetCore.Mvc;
using Semester4_CyberphysicalProject_API.DMI_Objects;
using Semester4_CyberphysicalProject_API.Models;
using Semester4_CyberphysicalProject_API.Services.Interfaces;

namespace Semester4_CyberphysicalProject_API.Controllers
{
    /// <summary>
    /// The main controller for the weather display page.
    /// Responsible for reading and displaying sea water temperature data.
    /// Only ever talks to IWeatherDisplay_Facade and IWeatherFetch_Facade —
    /// never directly to the database or DMI services.
    /// </summary>
    public class WeatherController : Controller
    {

        // Default history range constants.
        private const int HOURS_24 = 24;
        private const int HOURS_7_DAYS = 168;
        private const string RANGE_24H = "24h";
        private const string RANGE_7D = "7d";

        // Configuration key constants.
        private const string CONFIG_FETCH_INTERVAL_MINUTES = "Dmi:FetchIntervalMinutes";

        // Default fetch interval in minutes, used if the key is not found in appsettings.json.
        private const int DEFAULT_FETCH_INTERVAL_MINUTES = 15;




        // Read Only

        /// <summary>
        /// The display facade, used for reading and displaying data.
        /// </summary>
        private readonly IWeatherDisplay_Facade _displayFacade;

        /// <summary>
        /// The fetch facade, used for triggering a manual fetch cycle.
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
        /// Fetches readings, warmest reading, swimming verdict, and tracked stations
        /// from the display facade and passes them to the view.
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
            SwimmingVerdict_Enum? verdict = warmestReading != null
                ? this._displayFacade.GetSwimmingVerdict(warmestReading.WaterTemperature)
                : null;

            // Pass all data to the view via ViewBag.
            ViewBag.Readings = readings;
            ViewBag.WarmestReading = warmestReading;
            ViewBag.TrackedStations = trackedStations;
            ViewBag.Verdict = verdict;
            ViewBag.Range = range;
            ViewBag.Hours = hours;
            ViewBag.LatestObservedAt = latestObservedAt;
            ViewBag.TooColdBelow = this._config.GetValue<double>("Dmi:SwimmingThresholds:TooColdBelow", 17.0);
            ViewBag.BraveBelow = this._config.GetValue<double>("Dmi:SwimmingThresholds:BraveSwimmersBelow", 20.0);
            ViewBag.FetchIntervalMinutes = this._config.GetValue<int>("Dmi:FetchIntervalMinutes", 15);

            return View();
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
        /// The current time range — preserved across the refresh so the user
        /// stays on the same view they were on before clicking refresh.
        /// </param>
        public async Task<IActionResult> Refresh(string range = RANGE_24H)
        {
            // Read the fetch interval from appsettings.json.
            // Defaults to 15 minutes if not configured.
            int intervalMinutes = this._config.GetValue<int>(CONFIG_FETCH_INTERVAL_MINUTES, DEFAULT_FETCH_INTERVAL_MINUTES);

            // Get the most recent ObservedAt timestamp from the database.
            DateTime? latestObservedAt = await this._displayFacade.GetLatestObservedAtAsync();

            // If readings exist, check if enough time has passed since the last fetch.
            if (latestObservedAt != null)
            {
                // Calculate how many minutes have passed since the last reading.
                double minutesSinceLastFetch = (DateTime.UtcNow - latestObservedAt.Value).TotalMinutes;

                if (minutesSinceLastFetch < intervalMinutes)
                {
                    // Not enough time has passed — skip the fetch and redirect back.
                    Console.WriteLine($"[INFO] WeatherController.Refresh: Skipping fetch — only {minutesSinceLastFetch:F1} minutes since last fetch. Interval is {intervalMinutes} minutes.");
                    return RedirectToAction(nameof(Index), new { range });
                }
            }

            // Enough time has passed — trigger an immediate fetch cycle.
            await this._fetchFacade.FetchAndSaveLatestAsync();

            // Redirect back to the main page, preserving the current range.
            return RedirectToAction(nameof(Index), new { range });
        }





    }
}