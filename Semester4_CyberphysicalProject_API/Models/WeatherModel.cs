
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices.JavaScript;


namespace Semester4_CyberphysicalProject_API.Models
{

    /// <summary>
    /// Represents a single sea water temperature reading from a DMI observation station.
    /// Each instance of this class corresponds to one row in the "Readings" table in the SQL database.
    /// </summary>
    public class WaterTemperatureReading
    {
        
        public int Id { get; set; }   


        /// <summary>
        /// The unique identifier for the station as defined by DMI, e.g. "31616".
        /// Used when calling the DMI API to fetch readings for a specific station.
        /// </summary>
        [Required]
        public string StationId { get; set; } = string.Empty;    


        /// <summary>
        /// The name of the station, e.g. "Kerteminde".
        /// So the name is always available without a separate lookup.
        /// </summary>
        [Required]
        public string StationName { get; set; } = string.Empty;


        /// <summary>
        /// The sea water temperature in degrees Celsius recorded at this station.
        /// </summary>
        public double WaterTemperature { get; set; }   


        /// <summary>
        /// The exact date and time the temperature was observed, according to DMI's data.
        /// This is NOT the time we fetched the data — it is when the measurement was actually taken.
        /// </summary>
        public DateTime ObservedAt { get; set; }    


    }
}



