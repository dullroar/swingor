using swingor;
using System;
using Xunit;

namespace test_swingor
{
    public class Swingor_GetSubdirectory : IDisposable
    {
        public Swingor_GetSubdirectory()
        {
        }

        public void Dispose()
        {
        }

        [Fact]
        public void GetSubdirectory_Extracts_Subdirectory_Name()
        {
            Assert.Equal("/foo", swingor.Program.GetSubdirectory("/home/foo/Temp", "/home/foo/Temp/foo"));
        }
    }
}