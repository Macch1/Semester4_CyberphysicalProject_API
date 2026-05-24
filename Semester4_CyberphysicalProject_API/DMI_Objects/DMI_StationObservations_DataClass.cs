namespace Semester4_CyberphysicalProject_API.DMI_Objects
{
    /// <summary>
    /// Represents all observations collected from a single DMI station.
    /// Groups a list of DMI_Observation_DataClass instances under one station ID.
    /// </summary>
    public class DMI_StationObservations_DataClass
    {

        /// <summary>
        /// The unique station identifier as defined by DMI, e.g. "31616".
        /// Used as a shortcut for quick ID checks without inspecting individual observations.
        /// </summary>
        private string station_ID;

        /// <summary>
        /// The list of all observations recorded for this station.
        /// Each entry is one sea water temperature reading.
        /// </summary>
        private List<DMI_Observation_DataClass> DMI_Observations;






        /////////////////////////////////////////////////////////////////////////////////
        ///                             Constructors                                  ///
        /////////////////////////////////////////////////////////////////////////////////
        ///



        /// <summary>
        /// Creates a new station observations instance.
        /// Validates that the station ID matches the observations provided,
        /// then assigns the observations internally.
        /// </summary>
        /// <param name="station_ID">The unique DMI station identifier.</param>
        /// <param name="dmi_Observations">The list of observations for this station.</param>
        public DMI_StationObservations_DataClass(string station_ID, List<DMI_Observation_DataClass> dmi_Observations)
        {
            this.station_ID = station_ID;
            this.DMI_Observations = new List<DMI_Observation_DataClass>();

            // Validate that all observations belong to this station ID.
            bool isValid = StationIDsList_Match_List(station_ID, dmi_Observations);

            // Incase of problems with the Station ID's, we reject the the construction of the data-class. 
            // We want the Data to be trust-worthy, and not faulty. 
            if (!isValid)
            {
                // Print the problem to the console for debugging purposes.
                Console.WriteLine($"[ERROR] DMI_StationObservations_DataClass: " +
                    $"Observation mismatch detected. Expected all observations to belong " +
                    $"to station ID '{station_ID}', but one or more observations had a different station ID.");

                // Throw an exception to force the caller to fix the problem.
                // The program should never be allowed to continue with mismatched data.
                throw new InvalidOperationException(
                    $"Cannot create DMI_StationObservations_DataClass: " +
                    $"One or more observations do not belong to station ID '{station_ID}'. " +
                    $"The data structure must be consistent.");
            }

            // Populate the internal observations list.
            Assign_StationsObservations(dmi_Observations);
        }









        ////////////////////////////////////////////////////////////////////////////////////
        ///                             Private Methods                                  ///
        ////////////////////////////////////////////////////////////////////////////////////
        ///



        /// <summary>
        /// Validates that all observations in the list belong to the given station ID.
        /// If any observation has a different station ID, updates this instance's
        /// station_ID to match the majority, to stay consistent.
        /// </summary>
        /// <param name="station_ID">The expected station ID to validate against.</param>
        /// <param name="StationsObservations_List">The list of observations to check.</param>
        private bool StationIDsList_Match_List(string station_ID, List<DMI_Observation_DataClass> StationsObservations_List)
        {
            // Check that every observation in the list matches the expected station ID.
            foreach(DMI_Observation_DataClass observation in StationsObservations_List)
            {
                if(observation.Get_StationId() != station_ID)
                {
                    return false;
                }
            }

            return true;
        }



        /// <summary>
        /// Populates the internal observations list from the provided list.
        /// Called by the constructor after validation.
        /// </summary>
        /// <param name="StationsObservations_List">The list of observations to assign.</param>
        private bool Assign_StationsObservations(List<DMI_Observation_DataClass> StationsObservations_List)
        {
            // Assign all observations from the provided list to our internal list.
            this.DMI_Observations = StationsObservations_List;
            return true;
        }







        ///////////////////////////////////////////////////////////////////////////////////
        ///                             Public Methods                                  ///
        ///////////////////////////////////////////////////////////////////////////////////
        ///



        /// <summary>
        /// Returns the unique DMI station identifier for this group of observations.
        /// Used as a shortcut for quick ID checks without inspecting individual observations.
        /// </summary>
        /// <returns>The station ID as a string, e.g. "31616".</returns>
        public string Get_StationId()
        {
            return this.station_ID;
        }



        /// <summary>
        /// Returns all observations recorded for this station.
        /// </summary>
        /// <returns>The full list of DMI_Observation_DataClass instances for this station.</returns>
        public List<DMI_Observation_DataClass> Get_All_Observations()
        {
            return this.DMI_Observations;
        }




    }
}