using swingor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace swingor_sitemap
{
    public class Sitemap
    {
        // Create sitemap.xml file from HTML files in output directory. Obviously, this
        // needs to go after the processor that creates the HTML files.
        // directory: information about the directory to process, e.g., input directory,
        // output directory and the processor (transformation) to use.
        public static void ProcessSitemap(DirectoryToProcess directory)
        {
            directory = directory ?? throw new ArgumentNullException(nameof(directory));
            var myConfig = (from p in directory.Processors
                            where p.Class == $"{typeof(Sitemap)}" &&
                                  p.Method == nameof(ProcessSitemap)
                            select p).FirstOrDefault() ?? new Processor();

            if (Directory.Exists(directory.OutputPath))
            {
                var normalizedPath = Path.GetFullPath(directory.OutputPath);
                var outputFileName = Path.Combine(normalizedPath, "sitemap.xml");
                var sitemap = new StringBuilder(@"<?xml version='1.0' encoding='UTF-8'?>
<urlset xmlns='http://www.sitemaps.org/schemas/sitemap/0.9'>
");
                myConfig.Wildcards.ForEach(wc =>
                {
                    Directory.EnumerateFiles(normalizedPath, wc, SearchOption.AllDirectories)
                    .Except(myConfig.Exclusions.Select(e => Path.Combine(normalizedPath, e)))
                    .OrderBy(p => p)
                    .ToList()
                    .ForEach(htmlFileName =>
                    {
                        // Do the bare minimum escaping, because & the likeliest problematic
                        // file or path name character we're going to find.
                        sitemap.Append($"\t<url><loc>{directory.TargetURL.Replace("&", "&amp;")}/{Path.GetFileName(htmlFileName).Replace("&", "&amp;")}</loc></url>\n");
                    });
                });
                sitemap.Append("</urlset>");
                File.WriteAllText(outputFileName, sitemap.ToString());
            }
        }        
    }
}
