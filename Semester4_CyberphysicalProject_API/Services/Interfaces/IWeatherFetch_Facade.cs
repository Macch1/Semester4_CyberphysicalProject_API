namespace Semester4_CyberphysicalProject_API.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for the fetch side of the Weather Facade.
    /// This is the interface the Background Service uses — it only exposes
    /// fetch and store focused methods.
    ///
    /// DmiFetch_BackgroundService never talks to IDMI_Ocean_Service or
    /// IStationConfig_Service directly — it only ever sees this interface.
    /// </summary>
    public interface IWeatherFetch_Facade
    {


        ///////////////////////////////////////////////////////////////////////////////////
        ///                             Fetch Methods                                   ///
        ///////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Runs the full fetch cycle:
        /// - Reads the current station list
        /// - Fetches the latest observations from DMI for each station
        /// - Saves any new readings to the SQL database
        /// - Trims old readings from the SQL database if the limit is exceeded
        /// Called by DmiFetch_BackgroundService on every fetch interval.
        /// </summary>
        Task FetchAndSaveLatestAsync();


        /// <summary>
        /// Returns the most recent ObservedAt timestamp across all readings in the database.
        /// Used by DmiFetch_BackgroundService to check whether enough time has passed
        /// since the last fetch before triggering a new fetch cycle.
        /// Returns null if no readings exist in the database yet.
        /// </summary>
        /// <returns>
        /// The most recent ObservedAt timestamp as a nullable DateTime,
        /// or null if no readings exist.
        /// </returns>
        Task<DateTime?> GetLastFetchTimestampAsync();










        ///////////////////////////////////////////////////////////////////////////////////
        ///                            Discover Methods                                 ///
        ///////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Checks whether the local station list file exists.
        /// If it does not exist, InitialiseStationsAsync() should be called first.
        /// </summary>
        /// <returns>True if the station list file exists, false otherwise.</returns>
        bool StationsReady();


        /// <summary>
        /// Runs the station discovery process:
        /// - Calls the DMI Station API to find active stations in the bounding box
        /// - Filters to water temperature (tw) capable stations
        /// - Saves the result to the local station list file
        /// Called once on first boot, or after a reset.
        /// </summary>
        Task InitialiseStationsAsync();











        ///////////////////////////////////////////////////////////////////////////////////
        ///                             Reset Methods                                   ///
        ///////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Wipes all stored data and triggers a fresh station discovery.
        /// Called when the user requests a full reset from the UI or app settings.
        /// </summary>
        Task ResetAndRediscoverAsync();




    }
}