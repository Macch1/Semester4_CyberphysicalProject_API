using Semester4_CyberphysicalProject_API.DMI_Objects;




namespace Semester4_CyberphysicalProject_API.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for the DMI Ocean Service.
    /// Responsible for making HTTP calls to the DMI Observation API
    /// and returning the results as our internal data classes.
    /// 
    /// This interface is used by IWeatherFetchFacade internally.
    /// Nothing outside the Facade should call this directly.
    /// </summary>
    public interface IDMI_Ocean_Service
    {
        /// <summary>
        /// Fetches the latest sea water temperature observations from the DMI
        /// Observation API for a given list of stations.
        /// Returns the results as a DMI_StationsResponse_DataClass instance.
        /// Returns null if the fetch failed completely.
        /// </summary>
        /// <param name="stations">
        /// The list of stations to fetch observations for.
        /// Each station is identified by its DMI station ID.
        /// </param>
        /// <returns>
        /// A DMI_StationsResponse_DataClass containing all fetched observations,
        /// or null if the fetch failed completely.
        /// </returns>
        Task<DMI_StationsResponse_DataClass?> FetchLatestObservationsAsync(List<DMI_Station_DataClass> stations);
    }



}





