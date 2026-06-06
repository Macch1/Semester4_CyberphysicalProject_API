using Semester4_CyberphysicalProject_API.DMI_Objects;

namespace Semester4_CyberphysicalProject_API.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for the Station Config Service.
    /// Responsible for discovering which DMI stations are active in our
    /// configured geographic area, and managing the local station list.
    /// 
    /// This interface is used by IWeatherFetchFacade internally.
    /// Nothing outside the Facade should call this directly.
    /// </summary>
    public interface IStationConfig_Service
    {

        /// <summary>
        /// Checks whether the local station list file exists on disk.
        /// If false, DiscoverAndSaveStationsAsync() should be called.
        /// </summary>
        /// <returns>True if the station list file exists, false otherwise.</returns>
        bool StationFileExists();



        /// <summary>
        /// Reads the local station list file from disk and returns
        /// the stations as a list of DMI_Station_DataClass instances.
        /// Returns an empty list if the file does not exist or cannot be read.
        /// </summary>
        /// <returns>
        /// A list of DMI_Station_DataClass instances representing
        /// the tracked stations, or an empty list if none are found.
        /// </returns>
        List<DMI_Station_DataClass> ReadStations();



        /// <summary>
        /// Deletes the local station list file from disk.
        /// This forces a fresh discovery on the next startup or reset request.
        /// </summary>
        void DeleteStationFile();



        /// <summary>
        /// Calls the DMI Station API to discover all active stations
        /// within the configured geographic bounding box.
        /// Filters to only stations that measure water temperature (tw),
        /// deduplicates, and saves the result to the default station list file.
        /// Reads all constraints (min/max stations, file name) from appsettings.json.
        /// </summary>
        /// <returns>
        /// A list of DMI_Station_DataClass instances representing
        /// the newly discovered stations.
        /// </returns>
        Task<List<DMI_Station_DataClass>> DiscoverAndSaveStationsAsync();




        /// <summary>
        /// Checks whether a station with the given StationId exists
        /// in the local station list file.
        /// </summary>
        /// <param name="stationId">
        /// The unique DMI station identifier to check for.
        /// </param>
        /// <returns>
        /// True if the station exists in the file, false otherwise.
        /// </returns>
        bool CheckStation(string stationId);




        /// <summary>
        /// Reads and returns a single station from the local station list file,
        /// found by its StationId.
        /// </summary>
        /// <param name="stationId">
        /// The unique DMI station identifier of the station to retrieve.
        /// </param>
        /// <returns>
        /// The matching DMI_Station_DataClass instance,
        /// or null if no station with the given StationId was found.
        /// </returns>
        DMI_Station_DataClass? ReadStation(string stationId);




        /// <summary>
        /// Updates a single station's information in the local station list file.
        /// Finds the matching station by StationId and updates its StationName,
        /// Latitude, and longtitude from the provided DMI_Station_DataClass instance.
        /// The StationId itself is never changed — it is only used as the lookup key.
        /// </summary>
        /// <param name="updatedStation">
        /// A DMI_Station_DataClass instance containing the updated information.
        /// The StationId must match an existing station in the file.
        /// </param>
        /// <returns>
        /// True if the station was found and updated successfully, false otherwise.
        /// </returns>
        bool UpdateStation(DMI_Station_DataClass updatedStation);



        /// <summary>
        /// Deletes a single station from the local station list file.
        /// Finds the matching station by StationId and removes only that station,
        /// leaving all other stations in the file untouched.
        /// </summary>
        /// <param name="stationId">
        /// The unique DMI station identifier of the station to delete.
        /// </param>
        /// <returns>
        /// True if the station was found and deleted successfully, false otherwise.
        /// </returns>
        bool DeleteStation(string stationId);


    }
}