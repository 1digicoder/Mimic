using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using Mimic.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace UnitTests.Utils
{
    [TestClass]
    public class ParseContextTests
    {
        [TestMethod]
        public void ParseContext_CtorStreamReaderObject_SetsReaderAndStateProperties()
        {
            // Arrange
            var reader = new StreamReader(new MemoryStream());
            var state = new object();

            // Act
            var parseContext = new ParseContext(reader, state);

            // Assert
            Assert.AreSame(reader, parseContext.Reader);
            Assert.AreSame(state, parseContext.State);

            // Cleanup
            ((IDisposable)reader).Dispose();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ParseContext_CtorStreamReaderObject_ThrowsOnNullStreamReader()
        {
            // Arrange
            StreamReader nullReader = null;
            var state = new object();

            // Act
            var parseContext = new ParseContext(nullReader, state);
        }

        [TestMethod]
        public void ParseContext_ConvertToFromHeaderDictionary()
		{
			var h1 = new HeaderDictionary();
			h1.Add("A", new StringValues(new string[] { "ABC", "DEF" }));
			h1.Add("B", new StringValues(new string[] { "123", "456", "789" }));
			var from = ParseFunctions.ToJsonString(h1);
			System.Diagnostics.Debug.WriteLine($"{from}");
			var target = ParseFunctions.ToHeaderDictionary(from);
            
			Assert.AreEqual(2, target.Count);

		}

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ParseContext_CtorStreamReaderObject_ThrowsOnNullStateObject()
        {
            // Arrange
            var reader = new StreamReader(new MemoryStream());
            object nullState = null;

            // Act
            var parseContext = new ParseContext(reader, nullState);

            // Cleanup
            ((IDisposable)reader).Dispose();
        }
    }
}
