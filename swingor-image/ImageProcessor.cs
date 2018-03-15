using SixLabors.ImageSharp;
using SixLabors.ImageSharp.MetaData;
using SixLabors.ImageSharp.MetaData.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using swingor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace swingor_image
{
    public class ImageProcessor : BaseProcessor
    {
        // Recursively add copyright info to the EXIF metadata. These are done
        // in parallel and hence should be autonomous from each other.
        // Note that this processes files in the output directory, and does NOT update
        // the original input files.
        // directory: information about the directory to process, e.g., input directory,
        // output directory and the processor (transformation) to use.
        public static void ProcessImageExifs(DirectoryToProcess directory)
        {
            directory = directory ?? throw new ArgumentNullException(nameof(directory));
            var myConfig = (from p in directory.Processors
                            where p.Class == $"{typeof(ImageProcessor)}" &&
                                  p.Method == nameof(ProcessImageExifs)
                            select p).FirstOrDefault() ?? new Processor();

            if (Directory.Exists(directory.OutputPath))
            {
                var normalizedPath = Path.GetFullPath(directory.OutputPath);

                Parallel.ForEach(myConfig.Wildcards, wc =>
                {
                    Parallel.ForEach(Directory.EnumerateFiles(normalizedPath, wc, SearchOption.AllDirectories)
                                              .Except(myConfig.Exclusions.Select(e => Path.Combine(normalizedPath, e))), imgFileName =>
                    {
                        using (var img = Image.Load(imgFileName))
                        {
                            img.MetaData.ExifProfile = img.MetaData.ExifProfile ?? new ExifProfile();
                            var changed = false;
                            changed = img.AddExifTagIfMissing(ExifTag.Artist, directory.DefaultAuthor);
                            changed = img.AddExifTagIfMissing(ExifTag.Copyright, $"{directory.DefaultAuthor} - {DateTime.Now.Year}");

                            if (changed)
                            {
                                img.Save(imgFileName);
                            }
                        }
                    });
                });
            }
        }
    }

    public static class ImageExtensions
    {
        public static bool AddExifTagIfMissing(this Image<Rgba32> img, ExifTag tag, object value)
        {
            var changed = false;
            var tagValue = img.MetaData.ExifProfile.GetValue(tag);

            if (tagValue == null)
            {
                img.MetaData.ExifProfile.SetValue(tag, value);
                changed = true;
            }

            return changed;
        }
    }
}
