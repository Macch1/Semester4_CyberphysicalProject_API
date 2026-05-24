using System.Text.Json.Serialization;

namespace Semester4_CyberphysicalProject_API.DMI_Objects
{
    /// <summary>
    /// Represents a single sea water temperature observation from a DMI station.
    /// This is our internal data class — the rest of the project uses this,
    /// never the raw GeoJSON structure from DMI.
    /// </summary>
    public class DMI_Observation_DataClass
    {
        /// <summary>
        /// The unique station identifier as defined by DMI, e.g. "31616".
        /// </summary>
        private string station_ID;
        /// <summary>
        /// The human-readable name of the station, e.g. "Kerteminde".
        /// </summary>
        private string station_Name;
        /// <summary>
        /// The observed sea water temperature in degrees Celsius.
        /// </summary>
        private double value;
        /// <summary>
        /// The exact date and time the observation was recorded by DMI.
        /// This is NOT the time we fetched the data — it is when the measurement was actually taken.
        /// </summary>
        private DateTime value_TimeStamp;





        /////////////////////////////////////////////////////////////////////////////////
        ///                             Constructors                                  ///
        /////////////////////////////////////////////////////////////////////////////////
        ///



        /// <summary>
        /// Creates a new single observation data class instance with all required fields.
        /// </summary>
        /// <param name="id">The unique DMI station identifier.</param>
        /// <param name="name">The human-readable station name.</param>
        /// <param name="value">The observed sea water temperature in degrees Celsius.</param>
        /// <param name="value_TimeStamp">The date and time the observation was recorded by DMI.</param>
        public DMI_Observation_DataClass(string id, string name, double value, DateTime value_TimeStamp)
        {
            // Assign each field from the constructor parameters.
            this.station_ID = id;
            this.station_Name = name;
            this.value = value;
            this.value_TimeStamp = value_TimeStamp;
        }







        ///////////////////////////////////////////////////////////////////////////////////
        ///                             Public Methods                                  ///
        ///////////////////////////////////////////////////////////////////////////////////
        ///



        /// <summary>
        /// Returns the unique DMI station identifier for this observation.
        /// </summary>
        /// <returns>The station ID as a string, e.g. "31616".</returns>
        public string Get_StationId()
        {
            return this.station_ID;
        }


        /// <summary>
        /// Returns the human-readable name of the station for this observation.
        /// </summary>
        /// <returns>The station name as a string, e.g. "Kerteminde".</returns>
        public string Get_StationName()
        {
            return this.station_Name;
        }


        /// <summary>
        /// Returns the observed sea water temperature in degrees Celsius.
        /// </summary>
        /// <returns>The sea water temperature as a double.</returns>
        public double Get_Value()
        {
            return this.value;
        }


        /// <summary>
        /// Returns the date and time the observation was recorded by DMI.
        /// </summary>
        /// <returns>The observation timestamp as a DateTime.</returns>
        public DateTime Get_TimeStamp()
        {
            return this.value_TimeStamp;
        }



    }
}
