using System.Collections.Generic;

namespace swingor
{
    // Configuration information.
    public class AppConfiguration
    {
        public AppConfiguration()
        {
            DirectoriesToProcess = new List<DirectoryToProcess>();
        }

        // Clean (delete) all output directories before recreating them?
        public bool Clean { get; set; }
        // Run web server for testing?
        public bool Serve { get; set; }
        public List<DirectoryToProcess> DirectoriesToProcess { get; set; }
    }
}