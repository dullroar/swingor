using System.Collections.Generic;

namespace swingor
{
    // A given processor, e.g., assembly, class and method names.
    public class Processor
    {
        public Processor()
        {
            Exceptions = new List<string>();
        }
        
        public string DLL { get; set; }
        public string Class { get; set; }
        public string Method { get; set; }
        public List<string> Exceptions { get; set; }
    }
}