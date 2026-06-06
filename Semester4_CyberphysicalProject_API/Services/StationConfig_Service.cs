using System.Text.Json;
using Semester4_CyberphysicalProject_API.DMI_Objects;
using Semester4_CyberphysicalProject_API.Services.Interfaces;




namespace Semester4_CyberphysicalProject_API.Services
{
    /// <summary>
    /// Concrete implementation of IStationConfig_Service.
    /// Responsible for discovering which DMI stations are active in our
    /// configured geographic area, and managing the local station list file.
    /// </summary>
    public class StationConfig_Service : IStationConfig_Service
    {



        // DMI Json Const.

        /// <summary>
        /// The base URL for the DMI Station API endpoint.
        /// </summary>
        private const string DMI_API_STATION_BASE_URL = "https://dmigw.govcloud.dk/v2/oceanObs/collections/station/items";

        // The GeoJSON property string identifiers, used when parsing the DMI Station API response.
        private const string DMI_API_STRING_FEATURES = "features";
        private const string DMI_API_STRING_PROPERTIES = "properties";
        private const string DMI_API_STRING_PARAMETER_ID = "parameterId";

        // The DMI parameter ID for water temperature.
        private const string DMI_API_STRING_WATER_TEMPERATURE = "tw";

        // The DMI station property string identifiers.
        private const string DMI_API_STRING_STATION_ID = "stationId";
        private const string DMI_API_STRING_STATION_NAME = "stationName";
        private const string DMI_API_STRING_STATION_LONGTITUDE = "longtitude";
        private const string DMI_API_STRING_STATION_LATITUDE = "latitude";

        // The appsettings.json key strings for the bounding box coordinates.
        private const string DMI_BBOX_MIN_LONGTITUDE = "Dmi:BoundingBox:Minlongtitude";
        private const string DMI_BBOX_MAX_LONGTITUDE = "Dmi:BoundingBox:Maxlongtitude";
        private const string DMI_BBOX_MIN_LATITUDE = "Dmi:BoundingBox:MinLatitude";
        private const string DMI_BBOX_MAX_LATITUDE = "Dmi:BoundingBox:MaxLatitude";




        // Json File Const.

        // The JSON key strings used when reading and writing the local station list file.
        private const string JSON_FILE_STRING_STATION_ID = "stationId";
        private const string JSON_FILE_STRING_STATION_NAME = "stationName";
        private const string JSON_FILE_STRING_STATION_LONGTITUDE = "longtitude";
        private const string JSON_FILE_STRING_STATION_LATITUDE = "latitude";




        // Default Const.

        private const string DEFAULT_SAVEFILE_NAME = "DMI_Data_Stations";
        private const string DEFAULT_SAVEFOLDER_NAME = "Data";




        // Read Only

        /// <summary>
        /// Provides access to the appsettings.json configuration values.
        /// </summary>
        private readonly IConfiguration _config;

        /// <summary>
        /// The HttpClient used to make HTTP calls to the DMI Station API.
        /// </summary>
        private readonly HttpClient _http;

        /// <summary>
        /// The full path to the folder where the station list file is stored.
        /// </summary>
        private readonly string _dataFolder;

        /// <summary>
        /// The name of the station list file, including the .json extension.
        /// </summary>
        private readonly string _fileName;









        /////////////////////////////////////////////////////////////////////////////////
        ///                             Constructors                                  ///
        /////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Creates a new StationConfig_Service instance using default file and folder names.
        /// Receives HttpClient and IConfiguration from the DI container.
        /// </summary>
        /// <param name="http">The HttpClient used to call the DMI Station API.</param>
        /// <param name="config">Provides access to appsettings.json values.</param>
        public StationConfig_Service(HttpClient http, IConfiguration config)
        {
            this._config = config;
            this._http = http;

            this._fileName = DEFAULT_SAVEFILE_NAME + ".json";

            // Set the data folder to a "Data" subfolder in the current working directory.
            // This is where the station list file will be saved and read from.
            this._dataFolder = Path.Combine(Directory.GetCurrentDirectory(), DEFAULT_SAVEFOLDER_NAME);

            // Ensure the data folder exists — create it if it doesn't.
            Directory.CreateDirectory(this._dataFolder);
        }




        /// <summary>
        /// Creates a new StationConfig_Service instance using custom file and folder names.
        /// Receives HttpClient and IConfiguration from the DI container.
        /// </summary>
        /// <param name="fileName">The name of the station list file, without the .json extension.</param>
        /// <param name="folderName">The name of the folder to store the station list file in.</param>
        /// <param name="http">The HttpClient used to call the DMI Station API.</param>
        /// <param name="config">Provides access to appsettings.json values.</param>
        public StationConfig_Service(string fileName, string folderName, HttpClient http, IConfiguration config)
        {
            this._config = config;
            this._http = http;

            this._fileName = fileName + ".json";

            // Set the data folder to the specified subfolder in the current working directory.
            // This is where the station list file will be saved and read from.
            this._dataFolder = Path.Combine(Directory.GetCurrentDirectory(), folderName);

            // Ensure the data folder exists — create it if it doesn't.
            Directory.CreateDirectory(this._dataFolder);
        }










        ///////////////////////////////////////////////////////////////////////////////////
        ///                          Public File Methods                               ///
        ///////////////////////////////////////////////////////////////////////////////////


        /// <inheritdoc/>
        public bool StationFileExists()
        {
            // Check if the default station file exists on disk.
            return File.Exists(GetFilePath(this._fileName));
        }




        /// <inheritdoc/>
        public void DeleteStationFile()
        {
            string filePath = this.GetFilePath(this._fileName);

            if (!File.Exists(filePath))
            {
                Console.WriteLine("[WARNING] StationConfig_Service.DeleteStationFile: Station file not found — nothing to delete.");
                return;
            }

            // Delete the entire station list file from disk.
            File.Delete(filePath);
            Console.WriteLine("[INFO] StationConfig_Service.DeleteStationFile: Station file deleted successfully.");
        }











        ///////////////////////////////////////////////////////////////////////////////////
        ///                         Public Station Methods                             ///
        ///////////////////////////////////////////////////////////////////////////////////


        /// <inheritdoc/>
        public List<DMI_Station_DataClass> ReadStations()
        {
            // Use the default station file name.
            string filePath = this.GetFilePath(this._fileName);

            // If the file doesn't exist, return an empty list.
            if (!File.Exists(filePath))
            {
                Console.WriteLine("[WARNING] StationConfig_Service.ReadStations: Station file not found. Returning empty list.");
                return new List<DMI_Station_DataClass>();
            }

            try
            {
                // Read the raw JSON from the file.
                string json = File.ReadAllText(filePath);

                // Deserialise the JSON into a list of dictionaries,
                // then convert each one into a DMI_Station_DataClass instance.
                List<Dictionary<string, string>>? raw = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(json);

                if (raw == null)
                {
                    return new List<DMI_Station_DataClass>();
                }

                // Convert each dictionary entry into a DMI_Station_DataClass instance, and store them in "stations".
                List<DMI_Station_DataClass> stations = new List<DMI_Station_DataClass>();

                foreach (Dictionary<string, string> entry in raw)
                {
                    // Extract the station ID and name from the dictionary.
                    string id = entry.GetValueOrDefault(JSON_FILE_STRING_STATION_ID, string.Empty);
                    string name = entry.GetValueOrDefault(JSON_FILE_STRING_STATION_NAME, string.Empty);

                    // Try to extract coordinates if they exist, and record if it was successful.
                    bool hasLat = double.TryParse(entry.GetValueOrDefault(JSON_FILE_STRING_STATION_LATITUDE, null), out double lat);
                    bool hasLon = double.TryParse(entry.GetValueOrDefault(JSON_FILE_STRING_STATION_LONGTITUDE, null), out double lon);

                    // Create the station with or without coordinates.
                    if (hasLat && hasLon)
                    {
                        stations.Add(new DMI_Station_DataClass(id, name, lat, lon));
                    }
                    else
                    {
                        stations.Add(new DMI_Station_DataClass(id, name));
                    }
                }

                return stations;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] StationConfig_Service.ReadStations: Failed to read station file. {ex.Message}");
                return new List<DMI_Station_DataClass>();
            }
        }




        /// <inheritdoc/>
        public DMI_Station_DataClass? ReadStation(string stationId)
        {
            // Read all stations and find the one matching the given station ID.
            return this.ReadStations().FirstOrDefault(s => s.Get_StationId() == stationId);
        }




        /// <inheritdoc/>
        public bool CheckStation(string stationId)
        {
            // Read all stations and check if any match the given station ID.
            return this.ReadStations().Any(s => s.Get_StationId() == stationId);
        }




        /// <inheritdoc/>
        public bool UpdateStation(DMI_Station_DataClass updatedStation)
        {
            // Read the current station list from the file.
            List<DMI_Station_DataClass> stations = this.ReadStations();

            // Find the index of the station matching the given station ID.
            int index = stations.FindIndex(s => s.Get_StationId() == updatedStation.Get_StationId());

            if (index == -1)
            {
                Console.WriteLine($"[WARNING] StationConfig_Service.UpdateStation: Station '{updatedStation.Get_StationId()}' not found in station file.");
                return false;
            }

            // Replace the existing station with the updated one.
            stations[index] = updatedStation;

            // Save the updated list back to the file.
            return this.SaveStations(stations, this._fileName);
        }




        /// <inheritdoc/>
        public bool DeleteStation(string stationId)
        {
            // Read the current station list from the file.
            List<DMI_Station_DataClass> stations = this.ReadStations();

            // Find the station matching the given station ID.
            DMI_Station_DataClass? station = stations.FirstOrDefault(s => s.Get_StationId() == stationId);

            if (station == null)
            {
                Console.WriteLine($"[WARNING] StationConfig_Service.DeleteStation: Station '{stationId}' not found in station file.");
                return false;
            }

            // Remove the station from the list.
            stations.Remove(station);

            // Save the updated list back to the file.
            return this.SaveStations(stations, this._fileName);
        }











        ///////////////////////////////////////////////////////////////////////////////////
        ///                        Public Discovery Methods                            ///
        ///////////////////////////////////////////////////////////////////////////////////


        /// <inheritdoc/>
        public async Task<List<DMI_Station_DataClass>> DiscoverAndSaveStationsAsync()
        {
            // Read the maximum number of stations from appsettings.json.
            // Defaults to 1 if not configured.
            int maxStations = this._config.GetValue<int>("Dmi:MaxStations", 1);

            try
            {
                // Build the bounding box string from appsettings.json.
                string bbox = this.GetBboxFromConfig();

                // Build the DMI Station API URL with the bounding box filter.
                string url = $"{DMI_API_STATION_BASE_URL}?bbox={bbox}&status=Active&limit=100";

                Console.WriteLine($"[INFO] StationConfig_Service.DiscoverAndSaveStationsAsync: Fetching stations from DMI. URL: {url}");

                // Make the HTTP call to the DMI Station API.
                HttpResponseMessage response = await this._http.GetAsync(url);
                response.EnsureSuccessStatusCode();

                // Read the raw JSON response body.
                string json = await response.Content.ReadAsStringAsync();

                // Deserialise the GeoJSON response into a JsonElement for manual parsing.
                JsonElement collection = JsonSerializer.Deserialize<JsonElement>(json);
                JsonElement features = collection.GetProperty(DMI_API_STRING_FEATURES);

                // Filter and convert the raw station entries into DMI_Station_DataClass instances.
                List<DMI_Station_DataClass> stations = new List<DMI_Station_DataClass>();

                foreach (JsonElement feature in features.EnumerateArray())
                {
                    JsonElement props = feature.GetProperty(DMI_API_STRING_PROPERTIES);

                    // Only include stations that measure water temperature (tw).
                    bool hasTw = props.GetProperty(DMI_API_STRING_PARAMETER_ID).EnumerateArray().Any(p => p.GetString() == DMI_API_STRING_WATER_TEMPERATURE);

                    // Skip this station if it does not measure water temperature.
                    if (!hasTw)
                    {
                        continue;
                    }

                    // Extract the station ID and name from the properties block.
                    string id = props.GetProperty(DMI_API_STRING_STATION_ID).GetString() ?? string.Empty;
                    string name = props.GetProperty(DMI_API_STRING_STATION_NAME).GetString() ?? string.Empty;

                    // Skip stations with empty IDs.
                    if (string.IsNullOrEmpty(id))
                    {
                        continue;
                    }

                    // Skip duplicate station IDs.
                    if (stations.Any(s => s.Get_StationId() == id))
                    {
                        continue;
                    }

                    stations.Add(new DMI_Station_DataClass(id, name));

                    // Stop once we reach the maximum number of stations.
                    if (stations.Count >= maxStations)
                    {
                        break;
                    }
                }

                // Check if we found at least 1 station.
                if (stations.Count < 1)
                {
                    Console.WriteLine($"[WARNING] StationConfig_Service.DiscoverAndSaveStationsAsync: No stations were found in the configured bounding box.");
                }

                // Save the discovered stations to the default local file.
                this.SaveStations(stations, this._fileName);

                // Tells the console of its success and returns the stations.
                Console.WriteLine($"[INFO] StationConfig_Service.DiscoverAndSaveStationsAsync: Discovery complete. Found {stations.Count} stations. Saved to '{this._fileName}'.");
                return stations;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] StationConfig_Service.DiscoverAndSaveStationsAsync: Failed to discover stations. {ex.Message}");
                throw;
            }
        }












        ////////////////////////////////////////////////////////////////////////////////////
        ///                       Private File Helper Methods                           ///
        ////////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Builds the full file path for a given station list file name.
        /// Combines the data folder path with the provided file name.
        /// </summary>
        /// <param name="fileName">The name of the file, e.g. "DMI_Data_Stations.json".</param>
        /// <returns>The full file path as a string.</returns>
        private string GetFilePath(string fileName)
        {
            // Combines the folder path with the fileName.
            return Path.Combine(this._dataFolder, fileName);
        }




        /// <summary>
        /// Converts the given list of stations into a JSON string and saves it to the specified file.
        /// Used internally by UpdateStation, DeleteStation, and DiscoverAndSaveStationsAsync.
        /// </summary>
        /// <param name="stations">The list of stations to save.</param>
        /// <param name="fileName">The name of the file to save to.</param>
        /// <returns>True if saved successfully, false if an error occurred.</returns>
        private bool SaveStations(List<DMI_Station_DataClass> stations, string fileName)
        {
            try
            {
                // Manually build the JSON string from each station's getter methods.
                string stationFile_json_string = "[\n";

                // Loop through each station and build a JSON object for it.
                for (int i = 0; i < stations.Count; i++)
                {
                    // Get the current station from the list.
                    DMI_Station_DataClass station = stations[i];

                    // Open the JSON object for this station.
                    stationFile_json_string += "  {\n";

                    // Get the station ID and add it to the JSON object.
                    string station_id_temp = station.Get_StationId();
                    stationFile_json_string += $"    \"{JSON_FILE_STRING_STATION_ID}\": \"{station_id_temp}\",\n";

                    // Get the station name and add it to the JSON object.
                    // The name is already sanitised in DMI_Station_DataClass, so it is safe to use directly.
                    string station_name_temp = station.Get_StationName();
                    stationFile_json_string += $"    \"{JSON_FILE_STRING_STATION_NAME}\": \"{station_name_temp}\"";

                    // Only include coordinates if they have been set on this station.
                    if (station.Has_Coordinates())
                    {
                        // Add a comma after the station name, since more fields follow.
                        stationFile_json_string += ",\n";

                        // "System.Globalization.CultureInfo.InvariantCulture" == forces a decimal POINT (e.g. 55.5) instead of a decimal COMMA (e.g. 55,5).
                        // Convert the latitude to a string using InvariantCulture.
                        // The "!" is used here to silence the IDE null warning, which is frustrating to look at.
                        string station_latitude_temp = station.Get_Latitude()!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        stationFile_json_string += $"    \"{JSON_FILE_STRING_STATION_LATITUDE}\": {station_latitude_temp},\n";

                        // "System.Globalization.CultureInfo.InvariantCulture" == forces a decimal POINT (e.g. 55.5) instead of a decimal COMMA (e.g. 55,5).
                        // Convert the longtitude to a string using InvariantCulture.
                        // The "!" is used here to silence the IDE null warning, which is frustrating to look at.
                        string station_longtitude_temp = station.Get_Longtitude()!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        stationFile_json_string += $"    \"{JSON_FILE_STRING_STATION_LONGTITUDE}\": {station_longtitude_temp}\n";
                    }
                    else
                    {
                        // No coordinates available — close the station name line cleanly.
                        stationFile_json_string += "\n";
                    }

                    // Close the JSON object for this station.
                    stationFile_json_string += "  }";

                    // Add a comma after every entry except the last one,
                    // since JSON arrays require commas between elements, but not after the last one.
                    if (i < stations.Count - 1)
                    {
                        stationFile_json_string += ",";
                    }

                    // Move to the next line, ready for the next station.
                    stationFile_json_string += "\n";
                }

                // Close the JSON array.
                stationFile_json_string += "]";

                // Write the completed JSON string to the file, and returns true.
                File.WriteAllText(GetFilePath(fileName), stationFile_json_string);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] StationConfig_Service.SaveStations: Failed to save station file '{fileName}'. {ex.Message}");
                return false;
            }
        }












        ////////////////////////////////////////////////////////////////////////////////////
        ///                      Private URL Builder Methods                            ///
        ////////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Reads the bounding box coordinates from appsettings.json and formats
        /// them as a comma-separated string for use in the DMI Station API URL.
        /// Uses InvariantCulture to ensure decimal points are used regardless
        /// of the machine's locale setting — critical for Danish locale machines
        /// where the decimal separator is a comma by default.
        /// </summary>
        /// <returns>A bounding box string in the format "minLon,minLat,maxLon,maxLat".</returns>
        private string GetBboxFromConfig()
        {
            // Tries to read the Bbox coordinates from appsettings.json.
            // Falls back to default parameters if not configured.
            double minLon = this._config.GetValue<double>(DMI_BBOX_MIN_LONGTITUDE, 9.5);
            double maxLon = this._config.GetValue<double>(DMI_BBOX_MAX_LONGTITUDE, 12.5);
            double minLat = this._config.GetValue<double>(DMI_BBOX_MIN_LATITUDE, 54.5);
            double maxLat = this._config.GetValue<double>(DMI_BBOX_MAX_LATITUDE, 57.5);

            // "System.Globalization.CultureInfo.InvariantCulture" == forces a decimal POINT (e.g. 55.5) instead of a decimal COMMA (e.g. 55,5).
            // Returns a formatted string that fits the format defined by DMI.
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1},{2},{3}", minLon, minLat, maxLon, maxLat);
        }






    }
}