using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nerdbank.Streams;
using Newtonsoft.Json.Linq;

namespace Nezaboodka.Nevod.LanguageServer.Tests
{
    internal static class TestHelper
    {
        private static int s_nextRequestId = 1;

        internal static async Task TestResponseResultAsync(string method, string @params, string expectedResult)
        {
            // Arrange
            int requestId = GenerateRequestId();
            string expectedResponse = $@"{{""jsonrpc"": ""2.0"", ""id"": {requestId}, ""result"": {expectedResult}}}";
            (Stream inputWrite, Stream inputRead) = FullDuplexStream.CreatePair();
            (Stream outputWrite, Stream outputRead) = FullDuplexStream.CreatePair();

            // Act
            Task<int> serverProcessTask = new Server().ProcessAsync(outputWrite, inputRead);
            await SendRequestAsync(inputWrite, method, @params, requestId);
            string response = await ReceiveResponseAsync(outputRead) ?? throw new Exception("No response has been sent");
            await StopServerAsync(inputWrite);
            await serverProcessTask;
            inputWrite.Close(); // inputRead closed automatically
            outputWrite.Close(); // outputRead closed automatically

            // Assert
            AssertJsonEqual(expectedResponse, response);
        }

        private static async Task SendRequestAsync(Stream input, string method, string? @params, int requestId)
        {
            string body = $@"{{""jsonrpc"": ""2.0"", ""id"": {requestId}, ""method"": ""{method}"", ""params"": {@params ?? "null"}}}";
            string header = $"Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\n";
            StreamWriter inputWriter = new(input);
            await inputWriter.WriteAsync($"{header}\r\n{body}");
            await inputWriter.FlushAsync();
        }

        private static async Task<string?> ReceiveResponseAsync(Stream output)
        {
            StreamReader outputReader = new(output);
            string? contentLengthHeader = await outputReader.ReadLineAsync();
            if (contentLengthHeader is not null)
            {
                int contentLength = Convert.ToInt32(contentLengthHeader.Split(":")[1].Trim());
                await outputReader.ReadLineAsync(); // Skip \r\n after headers
                char[] response = new char[contentLength];
                await outputReader.ReadAsync(response, 0, contentLength);
                return new string(response);
            }
            else
                return null;
        }

        private static async Task StopServerAsync(Stream input)
        {
            await SendRequestAsync(input, "shutdown", null, GenerateRequestId());
            await SendRequestAsync(input, "exit", null, GenerateRequestId());
        }

        private static void AssertJsonEqual(string json1, string json2)
        {
            var token1 = JToken.Parse(json1);
            var token2 = JToken.Parse(json2);
            Assert.IsTrue(JToken.DeepEquals(token1, token2));
        }

        private static int GenerateRequestId() => Interlocked.Increment(ref s_nextRequestId);
    }
}
