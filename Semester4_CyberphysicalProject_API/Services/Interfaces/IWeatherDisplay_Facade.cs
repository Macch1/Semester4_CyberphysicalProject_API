using Semester4_CyberphysicalProject_API.DMI_Objects;
using Semester4_CyberphysicalProject_API.Models;

namespace Semester4_CyberphysicalProject_API.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for the display side of the Weather Facade.
    /// This is the interface the Controller uses — it only exposes
    /// read and display focused methods.
    /// 
    /// The Controller never talks to IDMI_Ocean_Service or IStationConfig_Service
    /// directly — it only ever sees this interface.
    /// </summary>
    public interface IWeatherDisplay_Facade
    {

        ///////////////////////////////////////////////////////////////////////////////////
        ///                             Data Methods                                    ///
        ///////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Returns the most recent water temperature readings for all tracked
        /// stations, up to the specified number of hours back in time.
        /// Used to populate the graph and data table in the view.
        /// </summary>
        /// <param name="hours">
        /// How many hours of history to retrieve, e.g. 24 for the last 24 hours.
        /// </param>
        /// <returns>
        /// A list of WaterTemperatureReading instances ordered by time,
        /// or an empty list if no readings are found.
        /// </returns>
        Task<List<WaterTemperatureReading>> GetReadingsAsync(int hours);


        /// <summary>
        /// Returns the single most recent reading with the highest water temperature
        /// across all tracked stations.
        /// Used to populate the hero card in the view.
        /// </summary>
        /// <returns>
        /// The warmest WaterTemperatureReading, or null if no readings exist.
        /// </returns>
        Task<WaterTemperatureReading?> GetWarmestReadingAsync();









        ///////////////////////////////////////////////////////////////////////////////////
        ///                            Station Methods                                  ///
        ///////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Returns the list of all currently tracked stations.
        /// Used when the view needs to show station names or other station info
        /// without fetching full observation data.
        /// </summary>
        /// <returns>
        /// A list of DMI_Station_DataClass instances,
        /// or an empty list if no stations are configured.
        /// </returns>
        List<DMI_Station_DataClass> GetTrackedStations();









        ///////////////////////////////////////////////////////////////////////////////////
        ///                            Verdict Methods                                  ///
        ///////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Returns a swimming suitability verdict based on the given temperature,
        /// using the thresholds configured in appsettings.json.
        /// </summary>
        /// <param name="temperature">
        /// The water temperature in degrees Celsius to evaluate.
        /// </param>
        /// <returns>
        /// A string verdict — one of:
        /// "Too cold", "Brave swimmers only", or "Perfect bathing temperature".
        /// </returns>
        SwimmingVerdict_Enum GetSwimmingVerdict(double temperature);





    }
}