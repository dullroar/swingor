using System.Collections.Generic;

namespace swingor
{
    // A given processor, e.g., assembly, class and method names.
    public class Processor
    {
        public Processor()
        {
            Exclusions = new List<string>();
            Prepends = new List<string>();
            Postpends = new List<string>();
            Wildcards = new List<string>();
        }
        
        // Class to load.
        public string Class { get; set; }
        // Path to optional config file for this processor.
        public string ConfigFilePath { get; set; }
        // Path to assembly DLL to load.
        public string DLL { get; set; }
        // File names or patterns to ignore.
        public List<string> Exclusions { get; set; }
        // Method to invoke.
        public string Method { get; set; }
        // Files to "prepend" to the output or otherwise pass to the processor before
        // processing each file.
        public List<string> Prepends { get; set; }
        // Files to "postpend" (append) to the output or otherwise pass to the processor
        // after processing each file.
        public List<string> Postpends { get; set; }
        // Number of files to process.
        public int? StopAfter { get; set; }
        // The wildcard to use on files in the input directory.
        public List<string> Wildcards { get; set; }
    }
}