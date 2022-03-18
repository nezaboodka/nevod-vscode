using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static Nezaboodka.Nevod.LanguageServer.Tests.TestHelper;

namespace Nezaboodka.Nevod.LanguageServer.Tests
{
    [TestClass]
    public class LanguageServerTests
    {
        [TestMethod]
        public async Task TestInitializeAsync()
        {
            string initializeParams = @"{
    ""processId"": 52332,
        ""clientInfo"": {
            ""name"": ""Visual Studio Code"",
            ""version"": ""1.58.2""
        },
        ""locale"": ""en"",
        ""rootPath"": null,
        ""rootUri"": null,
        ""capabilities"": { },
        ""trace"": ""off"",
        ""workspaceFolders"": null
}";
            string result = @"{
    ""capabilities"": {
        ""definitionProvider"": true,
        ""referencesProvider"": true,
        ""documentSymbolProvider"": true,
        ""textDocumentSync"": {
            ""openClose"": true,
            ""change"": 1
        },
        ""codeLensProvider"": {
            ""resolveProvider"": true
        },
        ""workspaceSymbolProvider"": true,
        ""renameProvider"": {
            ""prepareProvider"": true
        }
    }
}";
            await TestResponseResultAsync(method: "initialize", initializeParams, result);
        }
    }
}
