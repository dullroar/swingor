using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Binder;
using System;
using System.IO;

namespace swingor
{
    // Base class for processors, mostly to contain static utility methods useful across all.
    public class BaseProcessor
    {
        // Merge all configuration sources in the order of JSON, environment variables,
        // command line.
        // args: array of args in format typically passed to Main by runtime.
        // configFilePath: path to JSON configuration file.
        // Returns configuration loaded in AppConfiguration object.
        public static T GetConfiguration<T>(string configFilePath, string sectionName) where T : new()
        {
            configFilePath = configFilePath ?? throw new ArgumentNullException(nameof(configFilePath));
            sectionName = sectionName ?? throw new ArgumentNullException(nameof(sectionName));
            var config = new T();
            new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(configFilePath, true)
                .Build()
                .GetSection(sectionName)
                .Bind(config);
            return config;
        }

        // Pull out the remaining subdirectory path, removing any "original
        // directory" prefix.
        // originalPath: the path to extract from.
        // newPath: the portion of the path to extract.
        // Returns the extracted path.
        public static string GetSubdirectory(string originalPath, string newPath)
        {
            var normalizedOriginalPath = Path.GetFullPath(originalPath);
            var normalizedNewPath = Path.GetFullPath(newPath);
            return newPath.StartsWith(originalPath) ? newPath.Remove(0, originalPath.Length) : "";
        }
    }
}