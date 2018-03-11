using swingor_processors;
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
            Assert.Equal("/foo", BasicProcessors.GetSubdirectory("/home/foo/Temp", "/home/foo/Temp/foo"));
        }
    }
}