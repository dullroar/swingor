using swingor;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace test_swingor
{
    public class Swingor_PrepareOutputDirectories : IDisposable
    {
        public Swingor_PrepareOutputDirectories()
        {
        }

        public void Dispose()
        {
        }
        
        [Fact]
        public void PrepareOutputDirectories_AllDirectoriesExist()
        {
            var dirs = new List<string>()
            {
                "/home/foo/Temp2",
                "/home/foo/Temp2/bar"
            };
            
            dirs.ForEach(d =>
            {
                if (Directory.Exists(d))
                {
                    Directory.Delete(d, true);
                }
            });

            swingor.Program.PrepareOutputDirectories(dirs, false);

            dirs.ForEach(d =>
            {
                Assert.True(Directory.Exists(d));
            });
        }

        [Fact]
        public void PrepareOutputDirectories_CleanWorks()
        {
            var dirs = new List<string>()
            {
                "/home/foo/Temp2",
                "/home/foo/Temp2/bar"
            };
            
            dirs.ForEach(d =>
            {
                Directory.CreateDirectory(d);
                File.WriteAllText(Path.Combine(d, "test.txt"), "This is a test");
            });

            swingor.Program.PrepareOutputDirectories(dirs, true);

            dirs.ForEach(d =>
            {
                Assert.True(Directory.Exists(d));
                Assert.False(File.Exists(Path.Combine(d, "test.txt")));
            });
        }
    }
}