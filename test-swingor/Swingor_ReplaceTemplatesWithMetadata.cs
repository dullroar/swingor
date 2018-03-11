using swingor;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace test_swingor
{
    public class Swingor_ReplaceTemplatesWithMetadata : IDisposable
    {
        private Dictionary<string, string> Meta;

        public Swingor_ReplaceTemplatesWithMetadata()
        {
            Meta = new Dictionary<string, string>
            {
                { "author", "Elmer Fudd"},
                { "title", "This is a title" }
            };
        }

        public void Dispose()
        {
        }
        
        [Fact]
        public void ReplaceTemplatesWithMetadata_ExpectedResult()
        {
            var html = new StringBuilder(@"<body>
<h1>{{title}}</h1>
<p>{{author}}</p>
The rest of the page goes here.
</body>");
            swingor.Program.ReplaceTemplatesWithMetadata(html, Meta);
            Assert.Contains("<h1>This is a title</h1>", html.ToString());
            Assert.Contains("<p>Elmer Fudd</p>", html.ToString());
        }
    }
}