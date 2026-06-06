namespace Semester4_CyberphysicalProject_API.DMI_Objects
{
    /// <summary>
    /// Represents the swimming suitability verdict for a given water temperature.
    /// Used by the Facade and View to indicate whether conditions are suitable for swimming.
    /// </summary>
    public enum SwimmingVerdict_Enum
    {
        /// <summary>The water temperature is too cold for swimming.</summary>
        TooCold,

        /// <summary>The water temperature is suitable only for brave swimmers.</summary>
        BraveOnly,

        /// <summary>The water temperature is perfect for swimming.</summary>
        PerfectTemperature
    }
}