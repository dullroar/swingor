using swingor;
using System;
using Xunit;

namespace test_swingor
{
    public class Swingor_ParseYAML : IDisposable
    {
        public Swingor_ParseYAML()
        {
        }

        public void Dispose()
        {
        }
        
        [Fact]
        public void ParseYAML_ExpectedDefaults()
        {
            var md = @"---
title: This is a title
date: March 10, 2018
...
The rest of the markdown goes here.";
            var meta = swingor.Program.ParseYAML(md, "Elmer Fudd", "Default Title");
            Assert.Equal("Elmer Fudd", meta["author"]);
            Assert.Equal("", meta["subtitle"]);
            Assert.Equal(DateTime.Now.Year.ToString(), meta["copyright"]);
        }

        [Fact]
        public void ParseYAML_ExpectedValues()
        {
            var md = @"---
title: This is a title
subtitle: This is a subtitle
author: Daffy Duck
date: March 10, 2017
copyright: 2017
...
The rest of the markdown goes here.";
            var meta = swingor.Program.ParseYAML(md, "Elmer Fudd", "Default Title");
            Assert.Equal("This is a title", meta["title"]);
            Assert.Equal("This is a subtitle", meta["subtitle"]);
            Assert.Equal("Daffy Duck", meta["author"]);
            Assert.Equal("March 10, 2017", meta["date"]);
            Assert.Equal("2017", meta["copyright"]);
        }
    }
}