﻿using Markdig;
using Markdig.Extensions.Yaml;
using swingor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace swingor_processors
{
    public class BasicProcessors : BaseProcessor
    {
        // Process Markdown files. These are done in parallel and hence should be autonomous from
        // each other.
        // directory: information about the directory to process, e.g., input directory,
        // output directory and the processor (transformation) to use.
        public static void ProcessMarkdownFiles(DirectoryToProcess directory)
        {
            directory = directory ?? throw new ArgumentNullException(nameof(directory));
            var myConfig = (from p in directory.Processors
                            where p.Class == $"{typeof(BasicProcessors)}" &&
                                  p.Method == nameof(ProcessMarkdownFiles)
                            select p).FirstOrDefault() ?? new Processor();

            if (Directory.Exists(directory.InputPath))
            {
                var normalizedPath = Path.GetFullPath(directory.InputPath);
                var pipeline = new MarkdownPipelineBuilder().UseYamlFrontMatter().UseAdvancedExtensions().Build();

                Parallel.ForEach(myConfig.Wildcards, wc =>
                {
                    Parallel.ForEach(Directory.EnumerateFiles(normalizedPath, wc, SearchOption.AllDirectories)
                                              .Except(myConfig.Exclusions.Select(e => Path.Combine(normalizedPath, e))), inputFileName =>
                    {
                        var subdirectory = GetSubdirectory(normalizedPath, Path.GetDirectoryName(inputFileName));
                        var outputDirectory = Path.Combine(directory.OutputPath, subdirectory);
                        Directory.CreateDirectory(outputDirectory);
                        var outputFileName = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(inputFileName)}.html");

                        // Only process file if the output file doesn't exist or exists and is older than the input file.
                        if ((File.Exists(outputFileName) &&
                            File.GetLastWriteTimeUtc(outputFileName) < File.GetLastWriteTimeUtc(inputFileName)) ||
                            !File.Exists(outputFileName))
                        {
                            var outputText = new StringBuilder();
                            myConfig.Prepends.ForEach(file => outputText.Append(File.ReadAllText(Path.Combine(directory.InputPath, file))));
                            var fileText = File.ReadAllText(inputFileName);
                            var meta = ParseYAML(fileText, directory.DefaultAuthor, directory.DefaultTitle);
                            outputText.Append(Markdown.ToHtml(fileText, pipeline));
                            myConfig.Postpends.ForEach(file => outputText.Append(File.ReadAllText(Path.Combine(directory.InputPath, file))));
                            ReplaceTemplatesWithMetadata(outputText, meta);
                            File.WriteAllText(outputFileName, outputText.ToString());
                        }
                    });
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
            var myConfig = (from p in directory.Processors
                            where p.Class == $"{typeof(BasicProcessors)}" &&
                                  p.Method == nameof(ProcessStaticFiles)
                            select p).FirstOrDefault() ?? new Processor();

            if (Directory.Exists(directory.InputPath))
            {
                var normalizedPath = Path.GetFullPath(directory.InputPath);

                Parallel.ForEach(myConfig.Wildcards, wc =>
                {
                    Parallel.ForEach(Directory.EnumerateFiles(normalizedPath, wc, SearchOption.AllDirectories)
                                              .Except(myConfig.Exclusions.Select(e => Path.Combine(normalizedPath, e))), inputFileName =>
                    {
                        var subdirectory = GetSubdirectory(normalizedPath, Path.GetDirectoryName(inputFileName));
                        var outputDirectory = Path.Combine(directory.OutputPath, subdirectory);
                        Directory.CreateDirectory(outputDirectory);
                        var outputFileName = Path.Combine(outputDirectory, Path.GetFileName(inputFileName));

                        // Only process file if the output file doesn't exist or exists and is older than the input file.
                        if ((File.Exists(outputFileName) &&
                            File.GetLastWriteTimeUtc(outputFileName) < File.GetLastWriteTimeUtc(inputFileName)) ||
                            !File.Exists(outputFileName))
                        {
                            File.Copy(inputFileName, outputFileName, true);
                        }
                    });
                });
            }
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
        public static Dictionary<string, string> ParseYAML(string input, string defaultAuthor, string defaultTitle)
        {
            var meta = new Dictionary<string, string>()
            {
                { "author", defaultAuthor },
                { "title", defaultTitle },
                { "subtitle", "" },
                { "date", DateTime.Now.ToString("dddd, MMMM dd, yyyy") },
                { "copyright", DateTime.Now.ToString("yyyy")}
            };

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
            }

            return meta;
        }        
 
        // Using a dictionary of key/value pairs, replace "Moustache-like" instances of each
        // key in the data (e.g., {{Author}}) with the value.
        // data: data containing Moustache-like template markers.
        // meta: dictionary of key/value pairs.
        public static void ReplaceTemplatesWithMetadata(StringBuilder data, Dictionary<string, string> meta)
        {
            meta.ToList().ForEach(kv => data.Replace($"{{{{{kv.Key}}}}}", kv.Value));
        }
    }
}
