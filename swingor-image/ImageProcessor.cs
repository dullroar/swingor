using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Brushes;
using SixLabors.ImageSharp.MetaData;
using SixLabors.ImageSharp.MetaData.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
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

        // Add watermark to images. These are done in parallel and hence should be
        // autonomous from each other.
        // Note that this processes files in the output directory, and does NOT update
        // the original input files.
        // directory: information about the directory to process, e.g., input directory,
        // output directory and the processor (transformation) to use.
        public static void AddWatermark(DirectoryToProcess directory)
        {
            directory = directory ?? throw new ArgumentNullException(nameof(directory));
            var myConfig = (from p in directory.Processors
                            where p.Class == $"{typeof(ImageProcessor)}" &&
                                  p.Method == nameof(AddWatermark)
                            select p).FirstOrDefault() ?? new Processor();
            var font = SystemFonts.CreateFont("Ubuntu", 10);

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
                            if (img.MetaData.ExifProfile != null && img.MetaData.ExifProfile.GetValue(ExifTag.Copyright) != null)
                            {
                                using (var img2 = img.Clone(ctx =>
                                                            ctx.ApplyScalingWaterMarkSimple(font,
                                                                                            $"© {img.MetaData.ExifProfile.GetValue(ExifTag.Copyright).Value.ToString()}",
                                                                                            Rgba32.HotPink, 5)))
                                {
                                    img2.Save(imgFileName);
                                };
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

        // From https://github.com/SixLabors/Samples/blob/master/ImageSharp/DrawWaterMarkOnImage/Program.cs
        public static IImageProcessingContext<TPixel> ApplyScalingWaterMarkSimple<TPixel>(this IImageProcessingContext<TPixel> processingContext, Font font, string text, TPixel color, float padding)
            where TPixel : struct, IPixel<TPixel>
        {
            return processingContext.Apply(img =>
            {
                SizeF size = TextMeasurer.Measure(text, new RendererOptions(font));
                Font scaledFont = new Font(font, Math.Min(img.Width, img.Height) / 100 * font.Size);
                var pos = new PointF(img.Width - padding, img.Height - padding);
                img.Mutate(i => i.DrawText(text, scaledFont, color, pos, new TextGraphicsOptions(true)
                {
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom
                }));
            });
        }        
    }
}
