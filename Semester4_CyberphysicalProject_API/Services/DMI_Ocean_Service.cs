using System.Text.Json;
using Semester4_CyberphysicalProject_API.DMI_Objects;
using Semester4_CyberphysicalProject_API.Services.Interfaces;

namespace Semester4_CyberphysicalProject_API.Services
{
    /// <summary>
    /// Concrete implementation of IDMI_Ocean_Service.
    /// Responsible for making HTTP calls to the DMI Observation API
    /// and returning the results as our internal DMI_Objects data classes.
    /// </summary>
    public class DMI_Ocean_Service : IDMI_Ocean_Service
    {

        // DMI API Const.

        /// <summary>
        /// The base URL for the DMI Observation API endpoint.
        /// </summary>
        private const string DMI_API_OBSERVATION_BASE_URL = "https://dmigw.govcloud.dk/v2/oceanObs/collections/observation/items";

        // The GeoJSON property string identifiers, used when parsing the DMI API response.
        private const string DMI_API_STRING_FEATURES = "features";
        private const string DMI_API_STRING_GEOMETRY = "geometry";
        private const string DMI_API_STRING_COORDINATES = "coordinates";
        private const string DMI_API_STRING_PROPERTIES = "properties";
        private const string DMI_API_STRING_OBSERVED = "observed";
        private const string DMI_API_STRING_VALUE = "value";

        // The DMI parameter ID for water temperature.
        private const string DMI_API_STRING_WATER_TEMPERATURE = "tw";

        // The DMI API query parameter strings.
        private const string DMI_API_QUERY_PARAMETER_ID = "parameterId";
        private const string DMI_API_QUERY_STATION_ID = "stationId";
        private const string DMI_API_QUERY_SORT_ORDER = "sortorder";
        private const string DMI_API_QUERY_LIMIT = "limit";

        // The DMI API query parameter values.
        private const string DMI_API_QUERY_SORT_ORDER_VALUE = "observed,DESC";
        private const string DMI_API_QUERY_LIMIT_VALUE = "1";




        // Read Only

        /// <summary>
        /// The HttpClient used to make HTTP calls to the DMI Observation API.
        /// </summary>
        private readonly HttpClient _http;









        /////////////////////////////////////////////////////////////////////////////////
        ///                             Constructors                                  ///
        /////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Creates a new DMI_Ocean_Service instance.
        /// Receives HttpClient from the DI container.
        /// </summary>
        /// <param name="http">The HttpClient used to call the DMI Observation API.</param>
        public DMI_Ocean_Service(HttpClient http)
        {
            this._http = http;
        }











        ///////////////////////////////////////////////////////////////////////////////////
        ///                         Public Fetch Methods                               ///
        ///////////////////////////////////////////////////////////////////////////////////


        /// <inheritdoc/>
        public async Task<DMI_StationsResponse_DataClass?> FetchLatestObservationsAsync(List<DMI_Station_DataClass> stations)
        {
            // The list of station IDs we are fetching for.
            List<string> stationIds = new List<string>();

            // The list of successfully fetched station observations.
            List<DMI_StationObservations_DataClass> stationObservations = new List<DMI_StationObservations_DataClass>();

            foreach (DMI_Station_DataClass station in stations)
            {
                // Record this station's ID in the list.
                stationIds.Add(station.Get_StationId());

                try
                {
                    // Build the DMI Observation API URL for this station.
                    string url = this.BuildObservationUrl(station.Get_StationId());

                    Console.WriteLine($"[INFO] DMI_Ocean_Service.FetchLatestObservationsAsync: Fetching observations for station '{station.Get_StationId()}'. URL: {url}");

                    // Make the HTTP call to the DMI Observation API.
                    HttpResponseMessage response = await this._http.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    // Read the raw GeoJSON response body.
                    string json = await response.Content.ReadAsStringAsync();

                    // Convert the raw GeoJSON string into a DMI_StationObservations_DataClass instance.
                    // This is the only place in the project that knows about DMI's GeoJSON format.
                    DMI_StationObservations_DataClass? observations = this.ConvertGeoJson_To_StationObservations(station, json);

                    if (observations == null)
                    {
                        // Log a warning and continue to the next station.
                        // A single failing station must not stop the entire fetch cycle.
                        Console.WriteLine($"[WARNING] DMI_Ocean_Service.FetchLatestObservationsAsync: No observations returned for station '{station.Get_StationId()}'. Skipping.");
                        continue;
                    }

                    // Add the successfully fetched station observations to the list.
                    stationObservations.Add(observations);
                }
                catch (HttpRequestException ex)
                {
                    // Log the HTTP error and continue to the next station.
                    // A single failing station must not stop the entire fetch cycle.
                    Console.WriteLine($"[ERROR] DMI_Ocean_Service.FetchLatestObservationsAsync: HTTP error fetching station '{station.Get_StationId()}'. {ex.Message}");
                    continue;
                }
            }

            // If no stations returned any observations, return null.
            if (stationObservations.Count == 0)
            {
                Console.WriteLine($"[ERROR] DMI_Ocean_Service.FetchLatestObservationsAsync: No observations were returned for any station.");
                return null;
            }

            // Build and return a DMI_StationsResponse_DataClass from all fetched observations.
            return new DMI_StationsResponse_DataClass(stationIds, stationObservations);
        }











        ///////////////////////////////////////////////////////////////////////////////////
        ///                       Private Conversion Methods                           ///
        ///////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Converts a raw GeoJSON string from the DMI Observation API into a
        /// DMI_StationObservations_DataClass instance for a single station.
        /// This is the only method in the project that knows about DMI's GeoJSON format.
        /// If the GeoJSON cannot be parsed or contains no valid features, returns null.
        /// </summary>
        /// <param name="station">
        /// The station the GeoJSON was fetched for.
        /// Used directly for station ID and name — these are never re-extracted from the GeoJSON,
        /// since we are here to get observations, not to rediscover station information.
        /// </param>
        /// <param name="json">The raw GeoJSON string returned by the DMI API.</param>
        /// <returns>
        /// A DMI_StationObservations_DataClass instance containing the parsed observations,
        /// or null if the GeoJSON could not be parsed or contained no valid data.
        /// </returns>
        private DMI_StationObservations_DataClass? ConvertGeoJson_To_StationObservations(DMI_Station_DataClass station, string json)
        {
            try
            {
                // Deserialise the raw GeoJSON string into a JsonElement for manual parsing.
                JsonElement root = JsonSerializer.Deserialize<JsonElement>(json);
                JsonElement features = root.GetProperty(DMI_API_STRING_FEATURES);

                // The list of observations we will build from the GeoJSON features.
                List<DMI_Observation_DataClass> observations = new List<DMI_Observation_DataClass>();


                // .  
                foreach (JsonElement feature in features.EnumerateArray())
                {
                    // Try to extract the geometry block — contains the station coordinates.
                    // The geometry block is optional — not all features have coordinates.
                    if (feature.TryGetProperty(DMI_API_STRING_GEOMETRY, out JsonElement geometry))
                    {
                        // Try to extract the coordinates array from the geometry block.
                        if (geometry.TryGetProperty(DMI_API_STRING_COORDINATES, out JsonElement coordinates))
                        {
                            // Convert the coordinates JsonElement into an array for index-based access.
                            JsonElement[] coordinatesArray = coordinates.EnumerateArray().ToArray();

                            // Only extract coordinates if the array contains at least 2 values (longtitude and latitude).
                            // This prevents an IndexOutOfRangeException if DMI returns a malformed or empty coordinates array.
                            if (coordinatesArray.Length >= 2)
                            {
                                // DMI coordinates are in [longtitude, latitude] order.
                                double longtitude = coordinatesArray[0].GetDouble();
                                double latitude = coordinatesArray[1].GetDouble();

                                // Update the station coordinates if they have not been set yet.
                                if (!station.Has_Coordinates())
                                {
                                    station.Update_Coordinates(latitude, longtitude);
                                }
                            }
                        }
                    }


                    // Extract the properties block — contains the actual observation data.
                    if (!feature.TryGetProperty(DMI_API_STRING_PROPERTIES, out JsonElement props))
                    {
                        // Skip this feature if it has no properties block.
                        Console.WriteLine($"[WARNING] DMI_Ocean_Service.ConvertGeoJson_To_StationObservations: Feature for station '{station.Get_StationId()}' has no properties block. Skipping.");
                        continue;
                    }

                    // We already know the station ID and name from the station parameter.
                    // We are here to get observations, not to rediscover station information.
                    string stationId = station.Get_StationId();
                    string stationName = station.Get_StationName();

                    // Extract the water temperature value from the properties block.
                    double value = props.GetProperty(DMI_API_STRING_VALUE).GetDouble();

                    // Extract the observation timestamp from the properties block.
                    DateTime observed = props.GetProperty(DMI_API_STRING_OBSERVED).GetDateTime();

                    // Build a DMI_Observation_DataClass instance from the extracted values.
                    DMI_Observation_DataClass observation = new DMI_Observation_DataClass(stationId, stationName, value, observed);

                    // Add the successfully parsed observation to the list.
                    observations.Add(observation);
                }

                // If no valid observations were found, return null.
                if (observations.Count == 0)
                {
                    Console.WriteLine($"[WARNING] DMI_Ocean_Service.ConvertGeoJson_To_StationObservations: No valid observations found in GeoJSON for station '{station.Get_StationId()}'.");
                    return null;
                }

                // Build and return a DMI_StationObservations_DataClass from the parsed observations.
                return new DMI_StationObservations_DataClass(station.Get_StationId(), observations);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] DMI_Ocean_Service.ConvertGeoJson_To_StationObservations: Failed to parse GeoJSON for station '{station.Get_StationId()}'. {ex.Message}");
                return null;
            }
        }












        ///////////////////////////////////////////////////////////////////////////////////
        ///                       Private URL Builder Methods                          ///
        ///////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Builds the full DMI Observation API URL for a single station.
        /// Requests only the most recent water temperature reading,
        /// sorted by observation time descending so the newest is always first.
        /// </summary>
        /// <param name="stationId">The unique DMI station identifier to fetch observations for.</param>
        /// <returns>The fully constructed URL as a string.</returns>
        private string BuildObservationUrl(string stationId)
        {
            // Build the URL with query parameters that tell DMI exactly what to return:
            //
            // "parameterId=tw": only water temperature readings
            //
            // "stationId={id}": only from this specific station
            //
            // "sortorder=observed,DESC": newest reading first
            //
            // "limit=1": only return the single most recent reading
            //
            return $"{DMI_API_OBSERVATION_BASE_URL}" +
                   $"?{DMI_API_QUERY_PARAMETER_ID}={DMI_API_STRING_WATER_TEMPERATURE}" +
                   $"&{DMI_API_QUERY_STATION_ID}={stationId}" +
                   $"&{DMI_API_QUERY_SORT_ORDER}={DMI_API_QUERY_SORT_ORDER_VALUE}" +
                   $"&{DMI_API_QUERY_LIMIT}={DMI_API_QUERY_LIMIT_VALUE}";
        }





    }
}