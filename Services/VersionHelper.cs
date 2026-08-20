using System.Reflection;

namespace InventoryPOS.Services
{
    /// <summary>
    /// Provides access to the application version at runtime.
    /// The version is sourced from the assembly's InformationalVersion attribute
    /// (or Version as a fallback), so keeping the version in the .csproj file
    /// automatically flows to the UI.
    /// </summary>
    public static class VersionHelper
    {
        private static readonly string _cachedVersion;

        static VersionHelper()
        {
            try
            {
                var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

                // Prefer InformationalVersion (allows semver like "1.0.0-beta.1")
                var informational = assembly
                    .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                    .OfType<AssemblyInformationalVersionAttribute>()
                    .FirstOrDefault();
                if (informational != null && !string.IsNullOrWhiteSpace(informational.InformationalVersion))
                {
                    _cachedVersion = informational.InformationalVersion.Trim();
                }
                else
                {
                    _cachedVersion = assembly.GetName().Version?.ToString() ?? "0.0.0";
                }
            }
            catch
            {
                _cachedVersion = "0.0.0";
            }
        }

        /// <summary>
        /// Gets the application version string (e.g., "1.0.0").
        /// </summary>
        public static string AppVersion => _cachedVersion;
    }
}
