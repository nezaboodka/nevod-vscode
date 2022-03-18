using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nezaboodka.Nevod.Services.Tests
{
    [TestClass]
    public class LineMapTests
    {
        [TestMethod]
        public void TranslatePosition()
        {
            string code = GetCrlfString(@"#Url = {'http', 'https'} + '://' + Domain + ? Path + ? Query;
Domain = Word + [1+ '.' + Word + [0+ {Word, '_', '-'}]];
Path = '/' + [0+ {Word, '/', '_', '+', '-', '%'}];
Query = '?' + ? (QueryParam + [0+ '&' + QueryParam]);
QueryParam = Identifier + '=' + Identifier;
Identifier = {Alpha, AlphaNum, '_'} + [0+ {Word, '_'}];");

            var lineMap = new LineMap(code);

            var position1 = new Position(0, 36);
            Assert.AreEqual(36, lineMap.OffsetAt(position1));
            var position2 = new Position(1, 10);
            Assert.AreEqual(73, lineMap.OffsetAt(position2));
            var position3 = new Position(2, 19);
            Assert.AreEqual(140, lineMap.OffsetAt(position3));
            var position4 = new Position(3, 22);
            Assert.AreEqual(195, lineMap.OffsetAt(position4));
            var position5 = new Position(4, 36);
            Assert.AreEqual(264, lineMap.OffsetAt(position5));
            var position6 = new Position(5, 44);
            Assert.AreEqual(317, lineMap.OffsetAt(position6));
        }

        [TestMethod]
        public void TranslateOffset()
        {
            string code = GetCrlfString(
                @"#Url = {'http', 'https'} + '://' + Domain + ? Path + ? Query;
Domain = Word + [1+ '.' + Word + [0+ {Word, '_', '-'}]];
Path = '/' + [0+ {Word, '/', '_', '+', '-', '%'}];
Query = '?' + ? (QueryParam + [0+ '&' + QueryParam]);
QueryParam = Identifier + '=' + Identifier;
Identifier = {Alpha, AlphaNum, '_'} + [0+ {Word, '_'}];"
            );

            var lineMap = new LineMap(code);

            Assert.AreEqual(new Position(0, 36), lineMap.PositionAt(36));
            Assert.AreEqual(new Position(1, 10), lineMap.PositionAt(73));
            Assert.AreEqual(new Position(2, 19), lineMap.PositionAt(140));
            Assert.AreEqual(new Position(3, 22), lineMap.PositionAt(195));
            Assert.AreEqual(new Position(4, 36), lineMap.PositionAt(264));
            Assert.AreEqual(new Position(5, 44), lineMap.PositionAt(317));
        }

        private static string GetCrlfString(string str) =>
            str.IndexOf("\r\n", StringComparison.Ordinal) != -1 ? str : str.Replace("\n", "\r\n");
    }
}
