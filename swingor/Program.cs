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

        // Make sure all output directories exist.
        // directories: list of directory paths to check and create if necessary.
        // clean: whether to delete the directories before recreating them.
        public static void PrepareOutputDirectories(List<string> directories, bool clean = false)
        {
            directories = directories ?? throw new ArgumentNullException(nameof(directories));
            directories.ForEach(directory => { if (clean) DeleteDirectory(directory); });
            directories.ForEach(directory => Directory.CreateDirectory(directory));
        }
#endregion
    }
}