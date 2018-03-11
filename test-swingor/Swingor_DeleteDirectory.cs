using swingor;
using System;
using System.IO;
using Xunit;

namespace test_swingor
{
    public class Swingor_DeleteDirectory : IDisposable
    {
        private readonly string _directory;

        public Swingor_DeleteDirectory()
        {
            _directory = "/home/foo/TestDir";
            Directory.CreateDirectory(_directory);
        }

        public void Dispose()
        {
        }
        
        [Fact]
        public void DeleteDirectory_RemovesDirectory()
        {
            swingor.Program.DeleteDirectory(_directory);
            Assert.False(Directory.Exists(_directory));
        }
    }
}