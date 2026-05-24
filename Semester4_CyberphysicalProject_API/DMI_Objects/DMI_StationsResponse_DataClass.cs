using System.Linq;



namespace Semester4_CyberphysicalProject_API.DMI_Objects
{
    /// <summary>
    /// Represents the full response from DMI, containing observations from multiple stations.
    /// This is the top-level internal data class that the rest of the project uses.
    /// The converter method produces one instance of this class from the raw GeoJSON string.
    /// </summary>
    public class DMI_StationsResponse_DataClass
    {
        /// <summary>
        /// The list of station IDs included in this response.
        /// Used for quick lookups and validation.
        /// </summary>
        private List<string> Stations_IDs;

        /// <summary>
        /// A dictionary mapping each station ID to its full set of observations.
        /// Allows fast O(1) lookup of observations by station ID.
        /// </summary>
        private Dictionary<string, DMI_StationObservations_DataClass> StationsObservations_Map;





        /////////////////////////////////////////////////////////////////////////////////
        ///                             Constructors                                  ///
        /////////////////////////////////////////////////////////////////////////////////
        ///



        /// <summary>
        /// Creates a new stations response from an array of station observations.
        /// Validates that all station IDs match the observations, then populates the map.
        /// Prints to console and throws an exception if validation fails critically.
        /// </summary>
        /// <param name="StationsIDs_List">The expected list of station IDs.</param>
        /// <param name="StationsObservations_Array">The array of station observations.</param>
        public DMI_StationsResponse_DataClass(List<string> StationsIDs_List, DMI_StationObservations_DataClass[] StationsObservations_Array)
        {
            this.Stations_IDs = StationsIDs_List;
            this.StationsObservations_Map = new Dictionary<string, DMI_StationObservations_DataClass>();

            // Validate IDs and update Stations_IDs if any are missing.
            bool isValid = Assign_StationIDsList_Match_Array(StationsIDs_List, StationsObservations_Array);

            // Prints to console and throws an exception if validation fails critically.
            if (!isValid)
            {
                // Print the problem to the console for debugging purposes.
                Console.WriteLine($"[ERROR] DMI_StationsResponse_DataClass: " +
                    $"Station ID validation failed critically for the provided array. " +
                    $"The data structure is inconsistent and cannot be trusted.");

                // Throw an exception to force the caller to fix the problem.
                // The program should never be allowed to continue with inconsistent data.
                throw new InvalidOperationException(
                    $"Cannot create DMI_StationsResponse_DataClass: " +
                    $"Station ID validation failed critically for the provided array. " +
                    $"The data structure must be consistent.");
            }

            // Populate the dictionary from the array.
            Assign_StationsObservations(StationsObservations_Array);
        }



        /// <summary>
        /// Creates a new stations response from a list of station observations.
        /// Validates that all station IDs match the observations, then populates the map.
        /// Prints to console and throws an exception if validation fails critically.
        /// </summary>
        /// <param name="StationsIDs_List">The expected list of station IDs.</param>
        /// <param name="StationsObservations_List">The list of station observations.</param>
        public DMI_StationsResponse_DataClass(List<string> StationsIDs_List, List<DMI_StationObservations_DataClass> StationsObservations_List)
        {
            this.Stations_IDs = StationsIDs_List;
            this.StationsObservations_Map = new Dictionary<string, DMI_StationObservations_DataClass>();

            // Validate IDs and update Stations_IDs if any are missing.
            bool isValid = Assign_StationIDsList_Match_List(StationsIDs_List, StationsObservations_List);

            // Prints to console and throws an exception if validation fails critically. 
            if (!isValid)
            {
                // Print the problem to the console for debugging purposes.
                Console.WriteLine($"[ERROR] DMI_StationsResponse_DataClass: " +
                    $"Station ID validation failed critically for the provided list. " +
                    $"The data structure is inconsistent and cannot be trusted.");

                // Throw an exception to force the caller to fix the problem.
                // The program should never be allowed to continue with inconsistent data.
                throw new InvalidOperationException(
                    $"Cannot create DMI_StationsResponse_DataClass: " +
                    $"Station ID validation failed critically for the provided list. " +
                    $"The data structure must be consistent.");
            }

            // Populate the dictionary from the list.
            Assign_StationsObservations(StationsObservations_List);
        }






        ////////////////////////////////////////////////////////////////////////////////////
        ///                             Private Methods                                  ///
        ////////////////////////////////////////////////////////////////////////////////////
        ///



        /// <summary>
        /// Validates that all station IDs in StationsIDs_List have a matching entry in
        /// StationsObservations_Array. Updates this.Stations_IDs to reflect the actual
        /// set of valid station IDs found — removing missing ones and adding unexpected ones.
        /// </summary>
        /// <param name="StationsIDs_List">The expected list of station IDs.</param>
        /// <param name="StationsObservations_Array">The array of station observations to validate against.</param>
        /// <returns>True if the IDs matched perfectly, true with corrections if adjustments were made.</returns>
        private bool Assign_StationIDsList_Match_Array(List<string> StationsIDs_List, DMI_StationObservations_DataClass[] StationsObservations_Array)
        {
            // All IDs from the expected list.
            List<string> IDs_listed = new List<string>();
            // IDs from the expected list that were found in the observations array.
            List<string> IDs_listed_n_used = new List<string>();
            // IDs found in the observations array that were NOT in the expected list.
            List<string> additional_IDs = new List<string>();

            // Copy the expected IDs into our working list.
            IDs_listed.AddRange(StationsIDs_List);

            // Walk through each station observations entry in the array.
            foreach (DMI_StationObservations_DataClass stationObservations in StationsObservations_Array)
            {
                if (IDs_listed.Contains(stationObservations.Get_StationId()) && !IDs_listed_n_used.Contains(stationObservations.Get_StationId()))
                {
                    // This station ID was expected and hasn't been seen yet — mark it as found.
                    IDs_listed_n_used.Add(stationObservations.Get_StationId());
                }
                else if (!IDs_listed.Contains(stationObservations.Get_StationId()))
                {
                    // This station ID was NOT in the expected list — it is an unexpected extra.
                    additional_IDs.Add(stationObservations.Get_StationId());
                }
                else if (IDs_listed.Contains(stationObservations.Get_StationId()) && IDs_listed_n_used.Contains(stationObservations.Get_StationId()))
                {
                    // Duplicate found — print to console and throw.
                    Console.WriteLine($"[ERROR] Assign_StationIDsList_Match_Array: " +
                        $"Duplicate station ID '{stationObservations.Get_StationId()}' " +
                        $"found in StationsObservations_Array. Each station must appear exactly once.");

                    throw new InvalidOperationException(
                        $"Duplicate station ID '{stationObservations.Get_StationId()}' found in StationsObservations_Array. " +
                        $"Each station must appear exactly once.");
                }
            }

            if (IDs_listed.Count == IDs_listed_n_used.Count && additional_IDs.Count == 0)
            {
                // Perfect match — every expected ID has exactly one matching observations entry.
                // Assign Stations_IDs exactly as provided.
                this.Stations_IDs = IDs_listed;
                return true;
            }
            else if (IDs_listed.Count == IDs_listed_n_used.Count && additional_IDs.Count > 0)
            {
                // Unexpected extra stations found — print warning to console.
                Console.WriteLine($"[WARNING] Assign_StationIDsList_Match_Array: " +
                    $"{additional_IDs.Count} unexpected station ID(s) found in StationsObservations_Array " +
                    $"and added to Stations_IDs: {string.Join(", ", additional_IDs)}");

                this.Stations_IDs = IDs_listed_n_used.Concat(additional_IDs).ToList();
                return true;
            }
            else if (IDs_listed.Count > IDs_listed_n_used.Count)
            {
                // Some expected IDs had no matching observations entry — identify and remove them.
                List<string> missing_IDs = IDs_listed.Except(IDs_listed_n_used).ToList();

                // Missing stations found — print warning to console.
                Console.WriteLine($"[WARNING] Assign_StationIDsList_Match_Array: " +
                    $"{missing_IDs.Count} expected station ID(s) had no matching observations entry " +
                    $"and were removed from Stations_IDs: {string.Join(", ", missing_IDs)}");

                // Assign Stations_IDs without the missing IDs, but include any additional ones found.
                this.Stations_IDs = IDs_listed_n_used.Concat(additional_IDs).ToList();

                return true;
            }

            return false;
        }



        /// <summary>
        /// Validates that all station IDs in StationsIDs_List have a matching entry in
        /// StationsObservations_List. Updates this.Stations_IDs to reflect the actual
        /// set of valid station IDs found — removing missing ones and adding unexpected ones.
        /// </summary>
        /// <param name="StationsIDs_List">The expected list of station IDs.</param>
        /// <param name="StationsObservations_List">The list of station observations to validate against.</param>
        /// <returns>True if the IDs matched perfectly, true with corrections if adjustments were made.</returns>
        private bool Assign_StationIDsList_Match_List(List<string> StationsIDs_List, List<DMI_StationObservations_DataClass> StationsObservations_List)
        {
            // All IDs from the expected list.
            List<string> IDs_listed = new List<string>();
            // IDs from the expected list that were found in the observations list.
            List<string> IDs_listed_n_used = new List<string>();
            // IDs found in the observations list that were NOT in the expected list.
            List<string> additional_IDs = new List<string>();

            // Copy the expected IDs into our working list.
            IDs_listed.AddRange(StationsIDs_List);

            // Walk through each station observations entry in the list.
            foreach (DMI_StationObservations_DataClass stationObservations in StationsObservations_List)
            {
                if (IDs_listed.Contains(stationObservations.Get_StationId()) && !IDs_listed_n_used.Contains(stationObservations.Get_StationId()))
                {
                    // This station ID was expected and hasn't been seen yet — mark it as found.
                    IDs_listed_n_used.Add(stationObservations.Get_StationId());
                }
                else if (!IDs_listed.Contains(stationObservations.Get_StationId()))
                {
                    // This station ID was NOT in the expected list — it is an unexpected extra.
                    additional_IDs.Add(stationObservations.Get_StationId());
                }
                else if (IDs_listed.Contains(stationObservations.Get_StationId()) && IDs_listed_n_used.Contains(stationObservations.Get_StationId()))
                {
                    // Duplicate found — print to console and throw.
                    Console.WriteLine($"[ERROR] Assign_StationIDsList_Match_Array: " +
                        $"Duplicate station ID '{stationObservations.Get_StationId()}' " +
                        $"found in StationsObservations_Array. Each station must appear exactly once.");

                    throw new InvalidOperationException(
                        $"Duplicate station ID '{stationObservations.Get_StationId()}' found in StationsObservations_Array. " +
                        $"Each station must appear exactly once.");
                }
            }

            if (IDs_listed.Count == IDs_listed_n_used.Count && additional_IDs.Count == 0)
            {
                // Perfect match — every expected ID has exactly one matching observations entry.
                this.Stations_IDs = IDs_listed;
                return true;
            }
            else if (IDs_listed.Count == IDs_listed_n_used.Count && additional_IDs.Count > 0)
            {
                // Unexpected extra stations found — print warning to console.
                Console.WriteLine($"[WARNING] Assign_StationIDsList_Match_Array: " +
                    $"{additional_IDs.Count} unexpected station ID(s) found in StationsObservations_Array " +
                    $"and added to Stations_IDs: {string.Join(", ", additional_IDs)}");

                this.Stations_IDs = IDs_listed_n_used.Concat(additional_IDs).ToList();
                return true;
            }
            else if (IDs_listed.Count > IDs_listed_n_used.Count)
            {
                // Some expected IDs had no matching observations entry — identify and remove them.
                List<string> missing_IDs = IDs_listed.Except(IDs_listed_n_used).ToList();

                // Missing stations found — print warning to console.
                Console.WriteLine($"[WARNING] Assign_StationIDsList_Match_Array: " +
                    $"{missing_IDs.Count} expected station ID(s) had no matching observations entry " +
                    $"and were removed from Stations_IDs: {string.Join(", ", missing_IDs)}");

                // Assign Stations_IDs without the missing IDs, but include any additional ones found.
                this.Stations_IDs = IDs_listed_n_used.Concat(additional_IDs).ToList();
                return true;
            }

            return false;
        }



        /// <summary>
        /// Populates the StationsObservations_Map dictionary from the provided array.
        /// Uses each station's ID as the dictionary key for fast O(1) lookups later.
        /// Skips any entry where the station ID is null or already exists in the map.
        /// Called by the constructor after validation has passed successfully.
        /// </summary>
        /// <param name="StationsObservations_Array">The array of station observations to assign.</param>
        /// <returns>True when the map has been successfully populated.</returns>
        private bool Assign_StationsObservations(DMI_StationObservations_DataClass[] StationsObservations_Array)
        {
            foreach (var stationObservations in StationsObservations_Array)
            {
                // Use the station ID as the dictionary key for fast lookups.
                var id = stationObservations.Get_StationId();
                if (id != null && !StationsObservations_Map.ContainsKey(id))
                {
                    StationsObservations_Map.Add(id, stationObservations);
                }
            }
            return true;
        }



        /// <summary>
        /// Populates the StationsObservations_Map dictionary from the provided list.
        /// Uses each station's ID as the dictionary key for fast O(1) lookups later.
        /// Skips any entry where the station ID is null or already exists in the map.
        /// Called by the constructor after validation has passed successfully.
        /// </summary>
        /// <param name="StationsObservations_List">The list of station observations to assign.</param>
        /// <returns>True when the map has been successfully populated.</returns>
        private bool Assign_StationsObservations(List<DMI_StationObservations_DataClass> StationsObservations_List)
        {
            foreach (var stationObservations in StationsObservations_List)
            {
                // Use the station ID as the dictionary key for fast lookups.
                var id = stationObservations.Get_StationId();
                if (id != null && !StationsObservations_Map.ContainsKey(id))
                {
                    StationsObservations_Map.Add(id, stationObservations);
                }
            }
            return true;
        }







        ///////////////////////////////////////////////////////////////////////////////////
        ///                             Public Methods                                  ///
        ///////////////////////////////////////////////////////////////////////////////////
        ///



        /// <summary>
        /// Checks whether a station with the given ID exists in this response.
        /// </summary>
        /// <param name="station_ID">The station ID to check for.</param>
        /// <returns>True if the station exists, false otherwise.</returns>
        public bool Check_Contains_Station(string station_ID)
        {
            return StationsObservations_Map.ContainsKey(station_ID);
        }



        /// <summary>
        /// Returns the list of all station IDs included in this response.
        /// </summary>
        /// <returns>A list of station ID strings.</returns>
        public List<string> Get_All_StationIDs()
        {
            return this.Stations_IDs;
        }



        /// <summary>
        /// Returns all observations from all stations as a flat list.
        /// Useful when you need to process every reading regardless of station.
        /// </summary>
        /// <returns>A flat list of all DMI_StationObservations_DataClass instances.</returns>
        public List<DMI_StationObservations_DataClass> Get_AllObservations_fromAllStations()
        {
            return StationsObservations_Map.Values.ToList();
        }



        /// <summary>
        /// Returns all observations from a specific station by its ID.
        /// </summary>
        /// <param name="station_ID">The station ID to retrieve observations for.</param>
        /// <returns>The DMI_StationObservations_DataClass for the given station, or null if not found.</returns>
        public DMI_StationObservations_DataClass? Get_AllObservations_fromStation(string station_ID)
        {
            // Return the station observations if found, or null if the station doesn't exist.
            return StationsObservations_Map.TryGetValue(station_ID, out var observations) ? observations : null;
        }



    }
}