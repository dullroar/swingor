using Markdig;
using Markdig.Extensions.Yaml;
using Microsoft.AspNetCore.Builder;  
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Binder;
using Microsoft.Extensions.Configuration.CommandLine;
using Microsoft.Extensions.FileProviders;
using swingor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace swingor
{
    public class Program
    {
        public static AppConfiguration Config { get; set; }

        // Process all the directories in the configuration:
        // 1. Make sure all the output directories exist (optionally deleting them first for "clean").
        // 2. Process each input directory to its corresponding output directory using the chosen processor.
        //    These are called in parallel, and hence should be autonomous in effect from each other.
        public static void Main(string[] args)
        {
            var Config = GetConfiguration(args, "appsettings.json");
            // Prepare all the things.
            var outputDirs = (from dir in Config.DirectoriesToProcess select dir.OutputPath).ToList();
            PrepareOutputDirectories(outputDirs, Config.Clean);
            // Do all the things.
            var tasks = (from dir in Config.DirectoriesToProcess select Task.Run(() => ProcessFiles(dir))).ToArray();
            Task.WaitAll(tasks);

            // Serve all the things.
            if (Config.Serve)
            {
                Serve(Config.DirectoriesToProcess.First().OutputPath);
            }
        }

#region FileProcessing
        // For each directory passed in, process it.
        // directory: information about the directory to process, e.g., input directory,
        // output directory and the processor (transformation) to use.
        public static void ProcessFiles(DirectoryToProcess directory)
        {
            directory = directory ?? throw new ArgumentNullException(nameof(directory));
            directory.Processors.ForEach(d =>
            {
                var a = Assembly.LoadFrom(d.DLL);
                var t = a.GetType(d.Class);
                var o = Activator.CreateInstance(t);
                var m = t.GetMethod(d.Method);
                m.Invoke(o, new object[] { directory });
            });
        }

        // Process Markdown files. These are done in parallel and hence should be autonomous from
        // each other.
        // directory: information about the directory to process, e.g., input directory,
        // output directory and the processor (transformation) to use.
        public static void ProcessMarkdownFiles(DirectoryToProcess directory)
        {
            directory = directory ?? throw new ArgumentNullException(nameof(directory));

            if (Directory.Exists(directory.InputPath))
            {
                var normalizedPath = Path.GetFullPath(directory.InputPath);
                // Note: At this time, the YamlFrontMatter extension doesn't seem to be working.
                var pipeline = new MarkdownPipelineBuilder().UseYamlFrontMatter().UseAdvancedExtensions().Build();

                Parallel.ForEach(Directory.EnumerateFiles(normalizedPath, directory.Wildcard, SearchOption.AllDirectories), inputFileName =>
                {
                    var subdirectory = GetSubdirectory(normalizedPath, Path.GetDirectoryName(inputFileName));
                    var outputDirectory = Path.Combine(directory.OutputPath, subdirectory);
                    Directory.CreateDirectory(outputDirectory);
                    var outputFileName = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(inputFileName)}.html");

                    if ((File.Exists(outputFileName) &&
                         File.GetLastWriteTimeUtc(outputFileName) < File.GetLastWriteTimeUtc(inputFileName)) ||
                        !File.Exists(outputFileName))
                    {
                        var outputText = new StringBuilder();
                        directory.Prepends.ForEach(file => outputText.Append(File.ReadAllText(Path.Combine(directory.InputPath, file))));
                        var fileText = File.ReadAllText(inputFileName);
                        (var strippedText, var meta) = ParseYAML(fileText, directory.DefaultAuthor, directory.DefaultTitle);
                        outputText.Append(Markdown.ToHtml(strippedText));
                        directory.Postpends.ForEach(file => outputText.Append(File.ReadAllText(Path.Combine(directory.InputPath, file))));
                        ReplaceTemplatesWithMetadata(outputText, meta);
                        File.WriteAllText(outputFileName, outputText.ToString());
                    }
                });
            }
        }

        // Recursively copy files by wildcard from input to output directory. These are done
        // in parallel and hence should be autonomous from each other.
        // directory: information about the directory to process, e.g., input directory,
        // output directory and the processor (transformation) to use.
        public static void ProcessStaticFiles(DirectoryToProcess directory)
        {
            directory = directory ?? throw new ArgumentNullException(nameof(directory));

            if (Directory.Exists(directory.InputPath))
            {
                var normalizedPath = Path.GetFullPath(directory.InputPath);

                Parallel.ForEach(Directory.EnumerateFiles(normalizedPath, directory.Wildcard, SearchOption.AllDirectories), inputFileName =>
                {
                    var subdirectory = GetSubdirectory(normalizedPath, Path.GetDirectoryName(inputFileName));
                    var outputDirectory = Path.Combine(directory.OutputPath, subdirectory);
                    Directory.CreateDirectory(outputDirectory);
                    var outputFileName = Path.Combine(outputDirectory, Path.GetFileName(inputFileName));

                    if ((File.Exists(outputFileName) &&
                         File.GetLastWriteTimeUtc(outputFileName) < File.GetLastWriteTimeUtc(inputFileName)) ||
                        !File.Exists(outputFileName))
                    {
                        File.Copy(inputFileName, outputFileName, true);
                    }
                });
            }
        }
#endregion

#region Web
        // Set up Kestrel server as a static file server.
        // app: application information.
        public void Configure(IApplicationBuilder app)
        {
            app.UseDefaultFiles();
            app.UseStaticFiles();
        }

        // Run Kestrel server as a static file server on the generated files.
        // root: path to web root to serve.
        public static void Serve(string root)
        {
            root = root ?? Directory.GetCurrentDirectory();            
            new WebHostBuilder()
                .UseContentRoot(root)
                .UseKestrel()
                .UseStartup<Program>()
                .UseWebRoot(root) // See Jeremy Thompson's answer here: https://stackoverflow.com/questions/46161382/asp-net-core-2-0-kestrel-not-serving-static-content
                .Build()
                .Run();
        }
#endregion

#region Utilities
        // Wrapper around System.IO.Directory.DeleteDirectory. Basically just makes
        // sure that a directory that doesn't exist is not an error (for example,
        // running "clean" when there hasn't been any prior output).
        // path: path of directory to delete.
        public static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        // Merge all configuration sources in the order of JSON, environment variables,
        // command line.
        // args: array of args in format typically passed to Main by runtime.
        // configFilePath: path to JSON configuration file.
        // Returns configuration loaded in AppConfiguration object.
        public static AppConfiguration GetConfiguration(string[] args, string configFilePath = "appsettings.json")
        {
            var config = new AppConfiguration();
            new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(configFilePath, true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build()
                .GetSection("AppConfiguration")
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

        // Quick and dirty INCOMPLETE YAML "parser" meant to extract VERY simple
        // metadata from YAML front matter in Markdown. The YAML front matter has
        // to be the very first lines in the Markdown.
        // Also returns the rest of the file contents after the YAML because Markdig's
        // YAML extension is not working for me.
        // input: data representing file contents.
        // defaultAuthor: value to assign to "author" key if one not found in YAML.
        // defaultTitle: value to assign to "title" key if one not found in YAML.
        // Returns dictionary of key/value pairs. For some "well-known" keys, if
        // none is found, reasonable substitutes are used.
        public static (string, Dictionary<string, string>) ParseYAML(string input, string defaultAuthor, string defaultTitle)
        {
            var meta = new Dictionary<string, string>()
            {
                { "author", defaultAuthor },
                { "title", defaultTitle },
                { "subtitle", "" },
                { "date", DateTime.Now.ToString("dddd, MMMM dd, yyyy") },
                { "copyright", DateTime.Now.ToString("yyyy")}
            };
            var output = "";

            using (var rdr = new StringReader(input))
            {
                var line = rdr.ReadLine();
                var yamlExists = !string.IsNullOrEmpty(line) && line == "---";

                while (yamlExists && (yamlExists = (line = rdr.ReadLine()) != null && line != "---" && line != "..."))
                {
                    var parts = line.Split(new char[] { ':' }, 2);

                    if (parts.Length == 2 && !parts[0].Trim().StartsWith('#'))
                    {
                        meta[parts[0].Trim()] = parts[1].Trim();
                    }
                }

                output = rdr.ReadToEnd();
            }

            return (output, meta);
        }

        // Make sure all output directories exist.
        // directories: list of directory paths to check and create if necessary.
        // clean: whether to delete the directories before recreating them.
        public static void PrepareOutputDirectories(List<string> directories, bool clean = false)
        {
            directories = directories ?? throw new ArgumentNullException(nameof(directories));
            directories.ForEach(directory => { if (clean) DeleteDirectory(directory); });
            directories.ForEach(directory => Directory.CreateDirectory(directory));
        }

        // Using a dictionary of key/value pairs, replace "Moustache-like" instances of each
        // key in the data (e.g., {{Author}}) with the value.
        // data: data containing Moustache-like template markers.
        // meta: dictionary of key/value pairs.
        public static void ReplaceTemplatesWithMetadata(StringBuilder data, Dictionary<string, string> meta)
        {
            meta.ToList().ForEach(kv => data.Replace($"{{{{{kv.Key}}}}}", kv.Value));
        }
#endregion
    }
}