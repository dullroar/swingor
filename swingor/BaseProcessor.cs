using System.IO;

namespace swingor
{
    // Base class for processors, mostly to contain static utility methods useful across all.
    public class BaseProcessor
    {
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