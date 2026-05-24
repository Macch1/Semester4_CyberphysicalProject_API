using System.Text.Json.Serialization;

namespace Semester4_CyberphysicalProject_API.DMI_Objects
{
    /// <summary>
    /// Represents a single DMI station, used as a lightweight reference object.
    /// Used when we need to identify or refer to a station without carrying
    /// the full weight of all its observations around with it.
    /// 
    /// Station identity (ID and name) comes from the setup process (Request Type 1).
    /// Coordinates (Latitude and Longitude) come from observations (Request Type 2),
    /// and can therefore be updated after construction.
    /// </summary>
    public class DMI_Station_DataClass
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
        /// The latitude coordinate of the station.
        /// Null if coordinates have not been received from observations yet.
        /// </summary>
        private double? latitude;

        /// <summary>
        /// The longitude coordinate of the station.
        /// Null if coordinates have not been received from observations yet.
        /// </summary>
        private double? longitude;


        /////////////////////////////////////////////////////////////////////////////////
        ///                             Constructors                                  ///
        /////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Creates a new station data class instance with identity only.
        /// Used during the setup process (Request Type 1), before any
        /// observation data has been received.
        /// Coordinates are left null and can be set later via Update_Coordinates().
        /// </summary>
        /// <param name="stationId">The unique DMI station identifier.</param>
        /// <param name="stationName">The human-readable station name.</param>
        public DMI_Station_DataClass(string stationId, string stationName)
        {
            this.station_ID = stationId;
            this.station_Name = stationName;
            this.latitude = null;
            this.longitude = null;
        }

        /// <summary>
        /// Creates a new station data class instance with identity and coordinates.
        /// Used when observation data (Request Type 2) is already available,
        /// and we want to store the coordinates immediately.
        /// </summary>
        /// <param name="stationId">The unique DMI station identifier.</param>
        /// <param name="stationName">The human-readable station name.</param>
        /// <param name="latitude">The latitude coordinate of the station.</param>
        /// <param name="longitude">The longitude coordinate of the station.</param>
        public DMI_Station_DataClass(string stationId, string stationName, double latitude, double longitude)
        {
            this.station_ID = stationId;
            this.station_Name = stationName;
            this.latitude = latitude;
            this.longitude = longitude;
        }


        ////////////////////////////////////////////////////////////////////////////////////
        ///                             Private Methods                                  ///
        ////////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Validates that the provided latitude and longitude are within
        /// the valid geographic ranges before they are assigned.
        /// Latitude must be between -90 and 90.
        /// Longitude must be between -180 and 180.
        /// </summary>
        /// <param name="latitude">The latitude value to validate.</param>
        /// <param name="longitude">The longitude value to validate.</param>
        /// <returns>True if both values are valid, false otherwise.</returns>
        private bool Validate_Coordinates(double latitude, double longitude)
        {
            // Validate that latitude is within the valid geographic range.
            if (latitude < -90 || latitude > 90)
            {
                Console.WriteLine($"[ERROR] DMI_Station_DataClass: " +
                    $"Invalid latitude value '{latitude}' for station '{station_ID}'. " +
                    $"Latitude must be between -90 and 90.");
                return false;
            }

            // Validate that longitude is within the valid geographic range.
            if (longitude < -180 || longitude > 180)
            {
                Console.WriteLine($"[ERROR] DMI_Station_DataClass: " +
                    $"Invalid longitude value '{longitude}' for station '{station_ID}'. " +
                    $"Longitude must be between -180 and 180.");
                return false;
            }

            return true;
        }


        ///////////////////////////////////////////////////////////////////////////////////
        ///                             Public Methods                                  ///
        ///////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Returns the unique DMI station identifier for this station.
        /// </summary>
        /// <returns>The station ID as a string, e.g. "31616".</returns>
        public string Get_StationId()
        {
            return this.station_ID;
        }

        /// <summary>
        /// Returns the human-readable name of this station.
        /// </summary>
        /// <returns>The station name as a string, e.g. "Kerteminde".</returns>
        public string Get_StationName()
        {
            return this.station_Name;
        }

        /// <summary>
        /// Returns the latitude coordinate of this station.
        /// Returns null if coordinates have not been set yet.
        /// Use Has_Coordinates() to check before calling this.
        /// </summary>
        /// <returns>The latitude as a nullable double.</returns>
        public double? Get_Latitude()
        {
            return this.latitude;
        }

        /// <summary>
        /// Returns the longitude coordinate of this station.
        /// Returns null if coordinates have not been set yet.
        /// Use Has_Coordinates() to check before calling this.
        /// </summary>
        /// <returns>The longitude as a nullable double.</returns>
        public double? Get_Longitude()
        {
            return this.longitude;
        }

        /// <summary>
        /// Checks whether this station has coordinates assigned.
        /// Coordinates are only available after Update_Coordinates() has been called,
        /// or if the full constructor was used.
        /// </summary>
        /// <returns>True if both latitude and longitude are set, false otherwise.</returns>
        public bool Has_Coordinates()
        {
            if(this.latitude == null || this.longitude == null)
            {
                return false;
            }

            return this.latitude.HasValue && this.longitude.HasValue;
        }

        /// <summary>
        /// Updates the latitude and longitude of this station.
        /// Called when observation data (Request Type 2) becomes available
        /// after the station was initially created during the setup process.
        /// Validates the coordinates before assigning them.
        /// </summary>
        /// <param name="latitude">The new latitude coordinate of the station.</param>
        /// <param name="longitude">The new longitude coordinate of the station.</param>
        /// <returns>True if the coordinates were valid and updated, false otherwise.</returns>
        public bool Update_Coordinates(double latitude, double longitude)
        {
            // Validate the coordinates before assigning them.
            bool isValid = Validate_Coordinates(latitude, longitude);
            if (!isValid)
            {
                // Validation failed — coordinates remain unchanged.
                Console.WriteLine($"[WARNING] DMI_Station_DataClass: " +
                    $"Coordinates for station '{station_ID}' were not updated " +
                    $"because the provided values failed validation.");
                return false;
            }

            // Coordinates are valid — update them.
            this.latitude = latitude;
            this.longitude = longitude;
            return true;
        }
    }
}