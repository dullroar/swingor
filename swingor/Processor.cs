using System.Collections.Generic;

namespace swingor
{
    // A given processor, e.g., assembly, class and method names.
    public class Processor
    {
        public Processor()
        {
            Exclusions = new List<string>();
        }
        
        // Path to assembly DLL to load.
        public string DLL { get; set; }
        // Class to load.
        public string Class { get; set; }
        // Method to invoke.
        public string Method { get; set; }
        // File names or patterns to ignore.
        public List<string> Exclusions { get; set; }
        // Number of files to process.
        public int? StopAfter { get; set; }
    }
}