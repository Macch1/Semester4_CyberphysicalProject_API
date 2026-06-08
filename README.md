# DMI Sea Water Temperature

An ASP.NET Core MVC web application that fetches real Danish sea water temperature data from the DMI Oceanographic Observation API, stores it in a local SQL Server database, and displays it as an interactive graph and table with swimming suitability verdicts.

Built as part of Semester 4 — Cyberphysical Systems.

---

## Screenshots

> **Hero card and countdown timer**
> *(screenshot here)*

> **Temperature graph with threshold lines**
> *(screenshot here)*

> **History table with swimming verdicts**
> *(screenshot here)*

---

## What it does

- Automatically discovers active DMI sea temperature stations within a configurable geographic bounding box
- Fetches the latest water temperature reading from each station on a configurable interval
- Stores all readings in a local SQL Server database — building a history over time
- Displays the warmest current station in a hero card with a swimming suitability verdict
- Shows a Chart.js temperature graph filterable by the last 24 hours or 7 days
- Shows a history table with the 100 most recent readings across all stations
- Colour codes everything consistently by swimming verdict: Too cold, Brave swimmers only, or Perfect bathing temperature
- Auto-refreshes the page when new data is expected, with a live countdown timer

---

## Getting started

### Requirements

- .NET 8 SDK
- SQL Server LocalDB (included with Visual Studio)
- Visual Studio 2022 or later

### Setup

1. Clone the repository
2. Open the solution in Visual Studio
3. Restore NuGet packages (Visual Studio does this automatically on build)
4. Run the application — the database and station list are created automatically on first run

No manual database setup or migration commands are needed. The app creates everything on startup.

---

## Configuration

All configurable settings live in `appsettings.json`. No source code changes are needed to customise the app.

```json
{
  "Refresh_Setup": false,
  "Dmi": {
    "FetchIntervalMinutes": 30,
    "MaxReadingsPerStation": 1000,
    "MaxStations": 10,
    "LocationName": "Denmark",
    "SwimmingThresholds": {
      "TooColdBelow": 18.0,
      "BraveSwimmersBelow": 22.0
    },
    "BoundingBox": {
      "Minlongtitude": 9.3,
      "Maxlongtitude": 11.5,
      "MinLatitude": 54.7,
      "MaxLatitude": 55.9
    }
  }
}
```

| Setting | Description |
|---|---|
| `Refresh_Setup` | Set to `true` to wipe all data and rediscover stations on next restart. **Must be manually set back to `false` after reset.** |
| `FetchIntervalMinutes` | How often the app fetches new readings from DMI |
| `MaxReadingsPerStation` | Maximum readings stored per station before old ones are trimmed |
| `MaxStations` | Maximum number of stations to track simultaneously |
| `LocationName` | Human-readable name for the monitored area |
| `TooColdBelow` | Temperature threshold in °C below which the verdict is Too cold |
| `BraveSwimmersBelow` | Temperature threshold in °C below which the verdict is Brave swimmers only |
| `BoundingBox` | Geographic area to discover stations within (longitude/latitude) |

### Changing the geographic area

Update the four `BoundingBox` values to cover any area of Denmark. After changing them, set `Refresh_Setup: true` and restart the app to rediscover stations for the new area.

A useful tool for finding coordinates: [bboxfinder.com](https://bboxfinder.com)

### Resetting the app

To wipe all stored data and start fresh:

1. Set `Refresh_Setup: true` in `appsettings.json`
2. Restart the application
3. Set `Refresh_Setup: false` again after the app starts

Leaving `Refresh_Setup: true` will wipe all data on every restart.

---

## Architecture

The application follows the ASP.NET Core MVC pattern with a Facade layer separating the controller and background service from the underlying data services.

```
WeatherController       ──► IWeatherDisplay_Facade
DmiFetch_BackgroundService ──► IWeatherFetch_Facade
         Both implemented by WeatherFacade (singleton)
                 |
     ____________|____________
     |                       |
IDMI_Ocean_Service    IStationConfig_Service
(DMI_Ocean_Service)   (StationConfig_Service)
     |                       |
DMI Observation API   DMI Station API + local JSON file
```

### Key design decisions

**Facade pattern** — `WeatherFacade` implements both `IWeatherDisplay_Facade` and `IWeatherFetch_Facade`. The controller only ever sees the display interface. The background service only ever sees the fetch interface. Neither knows about the other.

**IDbContextFactory** — `WeatherFacade` is registered as a singleton, but `WeatherContext` (EF Core) is scoped. Using `IDbContextFactory<WeatherContext>` allows the singleton facade to create and dispose a fresh database context per operation, avoiding lifetime conflicts.

**Station freshness check** — During discovery, each candidate station is checked against the DMI Observation API to verify it has reported data within the last 30 days. Stale stations (some listed as Active by DMI but not reporting since 2020) are excluded automatically.

**Fetch interval enforcement** — Both the controller (manual refresh) and the background service independently check the most recent `ObservedAt` timestamp in the database before fetching. If not enough time has passed since the last reading, the fetch is skipped. This ensures the configured interval is respected regardless of who triggers the fetch.

**Smart countdown timer** — The auto-refresh countdown starts from the remaining time until the next expected fetch, not from the full interval. This is calculated by subtracting the elapsed time since the last observation from the interval, so a page reload mid-cycle shows the correct remaining wait time.

---

## Project structure

```
Controllers/
  WeatherController.cs          - Main page and manual refresh actions
Data/
  WeatherContext.cs             - EF Core database context
DMI_Objects/
  DMI_Observation_DataClass.cs  - Single temperature reading from DMI
  DMI_StationObservations_DataClass.cs - All observations for one station
  DMI_StationsResponse_DataClass.cs    - Full DMI response
  DMI_Station_DataClass.cs     - Station reference (ID, name, coordinates)
  SwimmingVerdict_Enum.cs      - TooCold / BraveOnly / PerfectTemperature
Models/
  WaterTemperatureReading.cs   - Database entity for stored readings
Services/
  Interfaces/
    IDMI_Ocean_Service.cs      - Contract for DMI observation fetching
    IStationConfig_Service.cs  - Contract for station file management
    IWeatherDisplay_Facade.cs  - Interface for the controller
    IWeatherFetch_Facade.cs    - Interface for the background service
  DMI_Ocean_Service.cs         - Fetches observations from DMI API
  StationConfig_Service.cs     - Discovers and manages the station list
  WeatherFacade.cs             - Coordinates all services (singleton)
  DmiFetch_BackgroundService.cs - Timed background fetch loop
Views/
  Shared/_Layout.cshtml        - Shared page layout
  Weather/Index.cshtml         - Main weather page
wwwroot/css/
  site.css                     - Shared design system
  weather.css                  - Weather page specific styles
appsettings.json               - All configuration
```

---

## Technology stack

| Component | Technology |
|---|---|
| Web framework | ASP.NET Core 8 MVC |
| Language | C# |
| View engine | Razor (.cshtml) |
| Database | SQL Server LocalDB via Entity Framework Core 8 |
| Chart library | Chart.js 4.4.0 (CDN) |
| Chart time axis | chartjs-adapter-date-fns 3.0.0 (CDN) |
| Chart annotations | chartjs-plugin-annotation 3.0.1 (CDN) |
| External API | DMI Oceanographic Observation API (free, no key required) |
| Configuration | appsettings.json via IConfiguration |

---

## Data source

All temperature data comes from the [DMI Oceanographic Observation API](https://www.dmi.dk/friedata/dokumentation/oceanographic-observations-data) — a free, publicly available API operated by the Danish Meteorological Institute.

Water temperature is measured inside harbours at a depth of a few metres. Readings are not standardised and should be used with this in mind.

---

## License

Built for educational purposes as part of Semester 4 — Cyberphysical Systems.



---

## Author

Developed as a university exam project for **Komponentbaserede Systemer** at SDU, Spring 2026.


---

## Disclaimer

AI have been used in the project for the following tasks: *Updating Comments*, *Updating JavaDocs*, *Debugging*, *Update ReadMe*, and as a *RubberDuck*.

