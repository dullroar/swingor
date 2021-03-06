using System.Collections.Generic;

namespace swingor
{
    // All the info needed to process a given directory.
    public class DirectoryToProcess
    {
        public DirectoryToProcess()
        {
            DefaultAuthor = "";
            DefaultTitle = "";
            InputPath = ".";
            OutputPath = ".";
            Processors = new List<Processor>();
        }

        // To use in metadata when author isn't supplied in YAML front matter in Markdown.
        public string DefaultAuthor { get; set; }
        // To use in metadata when title isn't supplied in YAML front matter in Markdown.
        public string DefaultTitle { get; set; } 
        // The path to the directory to process.
        public string InputPath { get; set; }
        // The path to the directory the processed files will be written to.
        public string OutputPath { get; set; }
        // The method name to invoke for processing the directory.
        public List<Processor> Processors { get; set; }
        // The base URL that the files will ultimately be hosted under, if any.
        public string TargetURL { get; set; }
        // The title of the site (something for the browser tab).
        public string SiteTitle { get; set; }
        // Site description (for RSS feeds, etc.)
        public string SiteDescription { get; set; }
    }
}