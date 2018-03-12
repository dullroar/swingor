using swingor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace swingor_rss
{
    public class RSS
    {
        // Create rss.xml file from HTML files in output directory. This should typically
        // go after the processor that creates the HTML files, although it works off the
        // input files and just makes assumptions about the output.
        // directory: information about the directory to process, e.g., input directory,
        // output directory and the processor (transformation) to use.
        // exclusions: processor-specific list of patterns of files to ignore.
        // stopAfter: number of files to process.
        public static void ProcessRSSFeed(DirectoryToProcess directory)
        {
            directory = directory ?? throw new ArgumentNullException(nameof(directory));
            var myConfig = (from p in directory.Processors
                            where p.Class == $"{typeof(RSS)}" &&
                                  p.Method == nameof(ProcessRSSFeed)
                            select p).FirstOrDefault() ?? new Processor();
            var exclusions = myConfig.Exclusions;
            var stopAfter = myConfig.StopAfter;

            if (Directory.Exists(directory.OutputPath))
            {
                var normalizedPath = Path.GetFullPath(directory.OutputPath);
                var outputFileName = Path.Combine(normalizedPath, "rss.xml");
                var rss = new StringBuilder($@"<?xml version='1.0' encoding='UTF-8'?>
<rss version='2.0'>
    <channel>
        <title>{directory.SiteTitle.Replace("&", "&amp;")}</title>
        <description>{directory.SiteDescription.Replace("&", "&amp;")}</description>
        <link>{directory.TargetURL.Replace("&", "&amp;")}</link>
        <lastBuildDate>{DateTime.Now.ToString("R")}</lastBuildDate>
        <pubDate>{DateTime.Now.ToString("R")}</pubDate>
        <ttl>1800</ttl>'
");
                var inputDir = new DirectoryInfo(directory.InputPath);
                directory.Wildcard.ForEach(wc =>
                {
                    (from fi in inputDir.EnumerateFileSystemInfos(wc, SearchOption.AllDirectories)
                     where !exclusions.Contains(fi.Name)
                     select fi)
                    .OrderByDescending(fi => fi.LastWriteTimeUtc)
                    .Take(stopAfter.HasValue && stopAfter.Value > 0 ? stopAfter.Value : int.MaxValue)
                    .ToList()
                    .ForEach(fi =>
                    {                    
                        using (var hasher = MD5.Create())
                        {
                            // A bit of a hack - the RSS spec says the Guid should be static for any given
                            // article, even if it changes. So, using the input file name to compute a hash and
                            // using that for the article identifying Guid. If the file name changes, the output
                            // file name will also change, and IMHO, then the Guid should, too.
                            var guid = new Guid(hasher.ComputeHash(Encoding.UTF8.GetBytes(fi.Name))).ToString();
                            // Could use the ParseYAML method from the main program, but our needs here are
                            // simple (for now), and a bit specialized.
                            var lines = File.ReadAllLines(fi.FullName).ToList();
                            var title = lines.Where(l => l.StartsWith("title:")).FirstOrDefault();

                            // Look for YAML front matter for the title. If don't find it, look for first
                            // level-1 header. Both of these are a bit "fragile."
                            if (!string.IsNullOrEmpty(title))
                            {
                                title = title.Replace("title:", "").Trim();
                            }
                            else
                            {
                                title = lines.Where(l => l.StartsWith("# ")).FirstOrDefault();
                            }

                            if (!string.IsNullOrEmpty(title))
                            {
                                title = title.Replace("# ", "").Trim();
                            }
                            else
                            {
                                // Otherwise use the base file name.
                                title = Path.GetFileNameWithoutExtension(fi.Name);
                            }

                            var date = lines.Where(l => l.StartsWith("date:")).FirstOrDefault();

                            if (!string.IsNullOrEmpty(date))
                            {
                                date = date.Replace("date:", "").Trim();
                            }
                            else
                            {
                                date = fi.LastWriteTimeUtc.ToString("R");
                            }

                            rss.Append("\t\t<item>\n")
                               .Append($"\t\t\t<title>{title.Replace("&", "&amp;")}</title>\n")
                               .Append($"\t\t\t<link>{directory.TargetURL.Replace("&", "&amp;")}/{Path.GetFileNameWithoutExtension(fi.Name).Replace("&", "&amp;")}.html</link>\n")
                               .Append($"\t\t\t<guid isPermaLink='false'>{guid}</guid>\n")
                               .Append($"\t\t\t<pubDate>{date}</pubDate>\n")
                               .Append("\t\t</item>\n");
                        }    
                    });
                });

                rss.Append("\t</channel>\n</rss>");
                File.WriteAllText(outputFileName, rss.ToString());
            }
        }        
    }
}
