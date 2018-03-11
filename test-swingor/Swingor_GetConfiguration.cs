using Microsoft.Extensions.PlatformAbstractions;
using swingor;
using System;
using System.IO;
using Xunit;

namespace test_swingor
{
    public class Swingor_GetConfiguration : IDisposable
    {
        private string AppPath;

        public Swingor_GetConfiguration()
        {
            AppPath = PlatformServices.Default.Application.ApplicationBasePath;
        }

        public void Dispose()
        {
        }
        
        [Fact]
        public void GetConfiguration_LoadsConfiguration()
        {
            var config = swingor.Program.GetConfiguration(new string[] { }, Path.Combine(AppPath, "appsettings.json"));
            Assert.True(config.Clean);
        }
    }
}