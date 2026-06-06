using Microsoft.EntityFrameworkCore;
using Semester4_CyberphysicalProject_API.Data;
using Semester4_CyberphysicalProject_API.DMI_Objects;
using Semester4_CyberphysicalProject_API.Models;
using Semester4_CyberphysicalProject_API.Services.Interfaces;

namespace Semester4_CyberphysicalProject_API.Services
{
    /// <summary>
    /// Concrete implementation of both IWeatherDisplay_Facade and IWeatherFetch_Facade.
    /// Acts as the single coordination point between the services and the rest of the application.
    /// The Controller only ever sees IWeatherDisplay_Facade.
    /// DmiFetch_BackgroundService only ever sees IWeatherFetch_Facade.
    /// </summary>
    public class WeatherFacade : IWeatherDisplay_Facade, IWeatherFetch_Facade
    {

        // Configuration key constants.
        // These match the keys in appsettings.json exactly.
        private const string CONFIG_MAX_READINGS_PER_STATION = "Dmi:MaxReadingsPerStation";
        private const string CONFIG_TOO_COLD_BELOW = "Dmi:SwimmingThresholds:TooColdBelow";
        private const string CONFIG_BRAVE_SWIMMERS_BELOW = "Dmi:SwimmingThresholds:BraveSwimmersBelow";

        // Default configuration values, used if the key is not found in appsettings.json.
        private const int DEFAULT_MAX_READINGS_PER_STATION = 672;
        private const double DEFAULT_TOO_COLD_BELOW = 17.0;
        private const double DEFAULT_BRAVE_SWIMMERS_BELOW = 20.0;




        // Read Only

        /// <summary>
        /// The DMI Ocean Service, used for fetching observations from the DMI Observation API.
        /// </summary>
        private readonly IDMI_Ocean_Service _dmiOceanService;

        /// <summary>
        /// The Station Config Service, used for managing the local station list file.
        /// </summary>
        private readonly IStationConfig_Service _stationConfigService;

        /// <summary>
        /// The Context Class Instance, used for reading and writing to the SQL database.
        /// </summary>
        private readonly WeatherContext _context;

        /// <summary>
        /// Provides access to the appsettings.json configuration values.
        /// </summary>
        private readonly IConfiguration _config;










        /////////////////////////////////////////////////////////////////////////////////
        ///                             Constructors                                  ///
        /////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Creates a new WeatherFacade instance.
        /// All dependencies are given as parameters (DI).
        /// </summary>
        /// <param name="dmiOceanService">The DMI Ocean Service, given as a parameter (DI).</param>
        /// <param name="stationConfigService">The Station Config Service, given as a parameter (DI).</param>
        /// <param name="context">The Context Class Instance for the SQL database, given as a parameter (DI).</param>
        /// <param name="config">Provides access to appsettings.json values, given as a parameter (DI).</param>
        public WeatherFacade(IDMI_Ocean_Service dmiOceanService, IStationConfig_Service stationConfigService, WeatherContext context, IConfiguration config)
        {
            this._dmiOceanService = dmiOceanService;
            this._stationConfigService = stationConfigService;
            this._context = context;
            this._config = config;
        }











        ///////////////////////////////////////////////////////////////////////////////////
        ///           Public IWeatherDisplay_Facade — Data Methods                     ///
        ///////////////////////////////////////////////////////////////////////////////////


        /// <inheritdoc/>
        public async Task<List<WaterTemperatureReading>> GetReadingsAsync(int hours)
        {
            // Calculate the cutoff time based on the requested number of hours.
            DateTime cutoff = DateTime.UtcNow.AddHours(-hours);

            // Return all readings from the SQL database within the requested time range,
            // ordered from oldest to newest.
            return await this._context.Readings
                .Where(r => r.ObservedAt >= cutoff)
                .OrderBy(r => r.ObservedAt)
                .ToListAsync();
        }


        /// <inheritdoc/>
        public async Task<WaterTemperatureReading?> GetWarmestReadingAsync()
        {
            // Return the single reading with the highest water temperature across all stations.
            return await this._context.Readings
                .OrderByDescending(r => r.WaterTemperature)
                .FirstOrDefaultAsync();
        }


        /// <inheritdoc/>
        public SwimmingVerdict_Enum GetSwimmingVerdict(double temperature)
        {
            // Read the swimming thresholds from appsettings.json.
            // Falls back to default values if not configured.
            double tooColdBelow = this._config.GetValue<double>(CONFIG_TOO_COLD_BELOW, DEFAULT_TOO_COLD_BELOW);
            double braveSwimmersBelow = this._config.GetValue<double>(CONFIG_BRAVE_SWIMMERS_BELOW, DEFAULT_BRAVE_SWIMMERS_BELOW);

            // Return the swimming verdict based on the temperature and configured thresholds.
            if (temperature < tooColdBelow)
            {
                return SwimmingVerdict_Enum.TooCold;
            }
            else if (temperature < braveSwimmersBelow)
            {
                return SwimmingVerdict_Enum.BraveOnly;
            }
            else
            {
                return SwimmingVerdict_Enum.PerfectTemperature;
            }
        }










        ///////////////////////////////////////////////////////////////////////////////////
        ///           Public IWeatherDisplay_Facade — Station Methods                  ///
        ///////////////////////////////////////////////////////////////////////////////////


        /// <inheritdoc/>
        public List<DMI_Station_DataClass> GetTrackedStations()
        {
            // Read and return the current station list from the local station list file.
            return this._stationConfigService.ReadStations();
        }









        ///////////////////////////////////////////////////////////////////////////////////
        ///           Public IWeatherFetch_Facade — Fetch Methods                      ///
        ///////////////////////////////////////////////////////////////////////////////////


        /// <inheritdoc/>
        public async Task FetchAndSaveLatestAsync()
        {
            // Read the current station list from the local station list file.
            List<DMI_Station_DataClass> stations = this._stationConfigService.ReadStations();

            if (stations.Count == 0)
            {
                Console.WriteLine("[WARNING] WeatherFacade.FetchAndSaveLatestAsync: No stations configured. Skipping fetch.");
                return;
            }

            // Fetch the latest observations from the DMI Observation API for each station.
            DMI_StationsResponse_DataClass? response = await this._dmiOceanService.FetchLatestObservationsAsync(stations);

            if (response == null)
            {
                Console.WriteLine("[WARNING] WeatherFacade.FetchAndSaveLatestAsync: No observations returned from DMI. Skipping save.");
                return;
            }

            // Save the new readings to the SQL database, checking for duplicates.
            await this.SaveReadingsAsync(response);

            // Trim old readings from the SQL database if the limit is exceeded.
            await this.TrimReadingsAsync();
        }












        ///////////////////////////////////////////////////////////////////////////////////
        ///           Public IWeatherFetch_Facade — Discover Methods                   ///
        ///////////////////////////////////////////////////////////////////////////////////


        /// <inheritdoc/>
        public bool StationsReady()
        {
            // Check if the local station list file exists.
            return this._stationConfigService.StationFileExists();
        }


        /// <inheritdoc/>
        public async Task InitialiseStationsAsync()
        {
            // Run the station discovery process and save the result to the local station list file.
            await this._stationConfigService.DiscoverAndSaveStationsAsync();
        }


        /// <inheritdoc/>
        public async Task ResetAndRediscoverAsync()
        {
            // Load and delete all readings from the SQL database.
            List<WaterTemperatureReading> allReadings = await this._context.Readings.ToListAsync();
            this._context.Readings.RemoveRange(allReadings);
            await this._context.SaveChangesAsync();
            Console.WriteLine("[INFO] WeatherFacade.ResetAndRediscoverAsync: All readings deleted from the SQL database.");

            // Delete the local station list file, forcing a fresh discovery.
            this._stationConfigService.DeleteStationFile();
            Console.WriteLine("[INFO] WeatherFacade.ResetAndRediscoverAsync: Station list file deleted.");

            // Rediscover stations from scratch and save the result.
            await this._stationConfigService.DiscoverAndSaveStationsAsync();
            Console.WriteLine("[INFO] WeatherFacade.ResetAndRediscoverAsync: Reset complete. All data wiped and stations rediscovered.");
        }










        ////////////////////////////////////////////////////////////////////////////////////
        ///                       Private Database Methods                              ///
        ////////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Saves new readings from the DMI response to the SQL database.
        /// Checks for duplicate readings before saving — a duplicate is a reading
        /// with the same StationId and ObservedAt timestamp already in the database.
        /// All new readings are saved in a single database operation for efficiency.
        /// </summary>
        /// <param name="response">The full DMI response containing observations from all stations.</param>
        private async Task SaveReadingsAsync(DMI_StationsResponse_DataClass response)
        {
            // Get all station observations from the response.
            List<DMI_StationObservations_DataClass> allStationObservations = response.Get_AllObservations_fromAllStations();

            foreach (DMI_StationObservations_DataClass stationObservations in allStationObservations)
            {
                // Get all observations for this station.
                List<DMI_Observation_DataClass> observations = stationObservations.Get_All_Observations();

                foreach (DMI_Observation_DataClass observation in observations)
                {
                    // Check for a duplicate reading — same StationId and ObservedAt timestamp.
                    // This happens when the fetch cycle runs more frequently than DMI updates its data.
                    bool isDuplicate = await this._context.Readings.AnyAsync(r => r.StationId == observation.Get_StationId() && r.ObservedAt == observation.Get_TimeStamp().ToUniversalTime());

                    if (isDuplicate)
                    {
                        Console.WriteLine($"[INFO] WeatherFacade.SaveReadingsAsync: Duplicate reading detected for station '{observation.Get_StationId()}' at '{observation.Get_TimeStamp()}'. Skipping.");
                        continue;
                    }

                    // Convert the observation to a WaterTemperatureReading and add it to the database.
                    WaterTemperatureReading reading = ConvertToReading(observation);
                    this._context.Readings.Add(reading);
                    Console.WriteLine($"[INFO] WeatherFacade.SaveReadingsAsync: New reading saved for station '{observation.Get_StationId()}' at '{observation.Get_TimeStamp()}'.");
                }
            }

            // Save all new readings to the SQL database in a single operation.
            // Calling SaveChangesAsync once outside the loop is more efficient than calling it per reading.
            await this._context.SaveChangesAsync();
        }


        /// <summary>
        /// Trims old readings from the SQL database when the limit is exceeded.
        /// Finds the oldest ObservedAt timestamp that needs to be trimmed,
        /// then deletes ALL readings at or before that timestamp across ALL stations.
        /// This ensures we never end up with incomplete data from a specific point in time —
        /// if we trim 1 reading, we trim all readings made at that same time across all stations.
        /// </summary>
        private async Task TrimReadingsAsync()
        {
            // Read the maximum number of readings per station from appsettings.json.
            // Defaults to 672 (one week of 15-minute readings) if not configured.
            int maxReadingsPerStation = this._config.GetValue<int>(CONFIG_MAX_READINGS_PER_STATION, DEFAULT_MAX_READINGS_PER_STATION);

            // Find the station with the most readings to determine if trimming is needed.
            // Uses a nullable int to safely handle an empty database without throwing an exception.
            int? maxReadingCount = await this._context.Readings
                .GroupBy(r => r.StationId)
                .Select(g => (int?)g.Count())
                .MaxAsync();

            if (maxReadingCount == null || maxReadingCount <= maxReadingsPerStation)
            {
                // No trimming needed — all stations are within the limit.
                return;
            }

            // Calculate how many readings need to be trimmed for the busiest station.
            int trimCount = maxReadingCount.Value - maxReadingsPerStation;

            // Find the cutoff reading — the trimCount-th oldest reading in the database.
            // We use this reading's ObservedAt timestamp as our cutoff point.
            WaterTemperatureReading? cutoffReading = await this._context.Readings
                .OrderBy(r => r.ObservedAt)
                .Skip(trimCount - 1)
                .FirstOrDefaultAsync();

            if (cutoffReading == null)
            {
                return;
            }

            DateTime cutoffTimestamp = cutoffReading.ObservedAt;

            // Get ALL readings at or before the cutoff timestamp across ALL stations.
            // Trimming across all stations at once ensures we never end up with
            // incomplete data from a specific point in time.
            List<WaterTemperatureReading> readingsToTrim = await this._context.Readings
                .Where(r => r.ObservedAt <= cutoffTimestamp)
                .ToListAsync();

            // Remove all readings at or before the cutoff timestamp.
            this._context.Readings.RemoveRange(readingsToTrim);

            // Save all changes to the SQL database in a single operation.
            await this._context.SaveChangesAsync();

            Console.WriteLine($"[INFO] WeatherFacade.TrimReadingsAsync: Trimmed {readingsToTrim.Count} readings at or before '{cutoffTimestamp}'.");
        }










        ////////////////////////////////////////////////////////////////////////////////////
        ///                      Private Conversion Methods                             ///
        ////////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Converts a DMI_Observation_DataClass instance into a WaterTemperatureReading instance
        /// for storage in the SQL database.
        /// The ObservedAt timestamp is converted to UTC to ensure consistent storage.
        /// </summary>
        /// <param name="observation">The DMI observation to convert.</param>
        /// <returns>A WaterTemperatureReading instance ready to be saved to the SQL database.</returns>
        private WaterTemperatureReading ConvertToReading(DMI_Observation_DataClass observation)
        {
            return new WaterTemperatureReading
            {
                StationId = observation.Get_StationId(),
                StationName = observation.Get_StationName(),
                WaterTemperature = observation.Get_Value(),
                ObservedAt = observation.Get_TimeStamp().ToUniversalTime()
            };
        }
    }
}