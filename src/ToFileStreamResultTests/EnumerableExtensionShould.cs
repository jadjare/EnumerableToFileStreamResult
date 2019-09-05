using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using ToFileStreamResult;
using ToFileStreamResultTests.Fakes;

namespace ToFileStreamResultTests
{
    [TestClass]
    public class EnumerableExtensionShould
    {
        [TestMethod]
        public void Given_EmptyList_When_NoOptionsSet_Then_ReturnsJustPropertyNamesAsHeaders()
        {
            //ARRANGE
            var emptyPeople = new List<Person>();

            //ACT
            var result = emptyPeople.ToFileStreamResult();

            //ASSERT
            var actualBytes = new Byte[result.FileStream.Length];
            result.FileStream.Read(actualBytes, 0, actualBytes.Length);

            var actualLines = Encoding.UTF8.GetString(actualBytes).Split("\r\n");

            Assert.AreEqual(1, actualLines.Length, "The file should only contain 1 line.");
            foreach (var propertyInfo in typeof(Person).GetProperties())
            {
                Assert.IsTrue(actualLines.First().Split(",").Contains(propertyInfo.Name));
            }

        }

        [TestMethod]
        public void Given_EmptyList_When_NoHeadersOptionIsSet_Then_EmptyFileIsReturned()
        {
            //ARRANGE
            var emptyPeople = new List<Person>();

            //ACT
            var result = emptyPeople.ToFileStreamResult(options => options.UsePropertyNamesAsHeaders = false);

            //ASSERT
            Assert.AreEqual(0, result.FileStream.Length, "The file should be empty");
        }

        [TestMethod]
        public void Given_List_When_NoHeadersIsSet_Then_FileRowCountMatchesTheListCount()
        {
            //ARRANGE
            var emptyPeople = Person.GetFakePeople().ToList();

            //ACT
            var result = emptyPeople.ToFileStreamResult(options => options.UsePropertyNamesAsHeaders = false);

            //ASSERT
            var actualBytes = new Byte[result.FileStream.Length];
            result.FileStream.Read(actualBytes, 0, actualBytes.Length);

            var actualLines = Encoding.UTF8.GetString(actualBytes).Split("\r\n");

            Assert.AreEqual(emptyPeople.Count, actualLines.Length);
        }

        [TestMethod]
        public void Given_List_When_OutputToFileStream_Then_ContentMatches()
        {
            //ARRANGE
            const string expectedTitle = "The Mythical Man Month";
            const int expectedPageCount = 322;
            var books = new List<Book> { new Book() { Title = expectedTitle, PageCount = expectedPageCount } };

            //ACT
            var result = books.ToFileStreamResult(options => options.UsePropertyNamesAsHeaders = false);

            //ASSERT
            var actualBytes = new Byte[result.FileStream.Length];
            result.FileStream.Read(actualBytes, 0, actualBytes.Length);

            var actualLines = Encoding.UTF8.GetString(actualBytes).Split("\r\n");
            var actualLineData = actualLines.First().Split(",");
            Assert.AreEqual(2, actualLineData.Length);
            Assert.AreEqual(expectedTitle, actualLineData.First());
            Assert.AreEqual(expectedPageCount.ToString(), actualLineData.Last());
        }

        [TestMethod]
        public void Given_PropertyWithANullValue_When_OutputToFileStream_Then_NullIsReturnedAsEmptyString()
        {
            //ARRANGE
            const string expectedTitle = "";
            const int expectedPageCount = 322;
            var books = new List<Book> { new Book() { Title = null, PageCount = expectedPageCount } };

            //ACT
            var result = books.ToFileStreamResult(options => options.UsePropertyNamesAsHeaders = false);

            //ASSERT
            var actualBytes = new Byte[result.FileStream.Length];
            result.FileStream.Read(actualBytes, 0, actualBytes.Length);

            var actualLines = Encoding.UTF8.GetString(actualBytes).Split("\r\n");
            var actualLineData = actualLines.First().Split(",");
            Assert.AreEqual(2, actualLineData.Length);
            Assert.AreEqual(expectedTitle, actualLineData.First());
            Assert.AreEqual(expectedPageCount.ToString(), actualLineData.Last());
        }


        [TestMethod]
        public void Given_ClassWithNonPublicMembers_When_OutputToFileStream_Then_OnlyPublicPropertiesAreOutput()
        {
            //ARRANGE
            var peopleWithPrivateData = PersonWithPrivateData.GetFakePeople().ToList();

            //ACT
            var result = peopleWithPrivateData.ToFileStreamResult(options => options.UsePropertyNamesAsHeaders = false);

            //ASSERT
            var actualBytes = new Byte[result.FileStream.Length];
            result.FileStream.Read(actualBytes, 0, actualBytes.Length);

            var actualLines = Encoding.UTF8.GetString(actualBytes).Split("\r\n");
            for (var i = 0; i < actualLines.Length; i++)
            {
                var actualLineData = actualLines[i].Split(",");
                Assert.AreEqual(3, actualLineData.Length);
                Assert.IsTrue(actualLineData.Contains(peopleWithPrivateData[i].FirstName));
                Assert.IsTrue(actualLineData.Contains(peopleWithPrivateData[i].Surname));
                Assert.IsTrue(actualLineData.Contains(peopleWithPrivateData[i].Height.ToString()));
            }

        }

        [DataTestMethod]
        [DataRow("|")]
        [DataRow("\t")]
        public void Given_DelimiterOption_When_FileResultStreamReturned_Then_ContentsIsDelimitedBySpecifiedDelimiter(string expectedDelimiter)
        {
            //ARRANGE
            var peopleWithPrivateData = PersonWithPrivateData.GetFakePeople().ToList();

            //ACT
            var result = peopleWithPrivateData.ToFileStreamResult(options =>
            {
                options.Delimiter = expectedDelimiter;
                options.UsePropertyNamesAsHeaders = false;
            });

            //ASSERT
            var actualBytes = new Byte[result.FileStream.Length];
            result.FileStream.Read(actualBytes, 0, actualBytes.Length);

            var actualLines = Encoding.UTF8.GetString(actualBytes).Split("\r\n");
            Assert.AreEqual(peopleWithPrivateData.Count, actualLines.Length);

            for (var i = 0; i < actualLines.Length; i++)
            {
                var actualLineData = actualLines[i].Split(expectedDelimiter);
                Assert.AreEqual(3, actualLineData.Length);
            }

        }

        [DataTestMethod]
        [DataRow("\r\n")]
        [DataRow("-->\r\n")]
        [DataRow("\n")]
        [DataRow("<Next>")]
        public void Given_EndOfLineOption_When_FileResultStreamReturned_Then_EndOfLineIsDelimitedBySpecifiedEndOfLineString(string expectedEndOfLine)
        {
            //ARRANGE
            var peopleWithPrivateData = PersonWithPrivateData.GetFakePeople().ToList();

            //ACT
            var result = peopleWithPrivateData.ToFileStreamResult(options =>
            {
                options.EndOfLine = expectedEndOfLine;
                options.UsePropertyNamesAsHeaders = false;
            });

            //ASSERT
            var actualBytes = new Byte[result.FileStream.Length];
            result.FileStream.Read(actualBytes, 0, actualBytes.Length);

            var actualLines = Encoding.UTF8.GetString(actualBytes).Split(expectedEndOfLine);
            Assert.AreEqual(peopleWithPrivateData.Count, actualLines.Length);

        }

        [TestMethod]
        public void Given_FileDownloadNameOption_When_FileResultStreamReturned_Then_ResultStreamsFileDownloadNameIsSet()
        {
            //ARRANGE
            var peopleWithPrivateData = PersonWithPrivateData.GetFakePeople().ToList();

            //ACT
            var result = peopleWithPrivateData.ToFileStreamResult(options => options.FileDownloadName = "Test.csv");

            //ASSERT
            Assert.AreEqual("Test.csv", result.FileDownloadName);

        }


        [TestMethod]
        public void Given_ContentTypeOption_When_FileResultStreamReturned_Then_ResultStreamsContentTypeIsSet()
        {
            //ARRANGE
            var peopleWithPrivateData = PersonWithPrivateData.GetFakePeople().ToList();

            //ACT
            var result = peopleWithPrivateData.ToFileStreamResult(options => options.ContentType = "application/ms-excel");

            //ASSERT
            Assert.AreEqual("application/ms-excel", result.ContentType);

        }

        [TestMethod]
        public void Given_UseQuotedIdentifierOption_When_OutputToFileStream_Then_ContentIsWrappedInQuotes()
        {
            //ARRANGE
            var book1 = new Book() { Title = "The Mythical Man Month", PageCount = 322 };
            var book2 = new Book() { Title = "What it is to be a \"coder\"", PageCount = 0 };
            var expectedTitle1 = "\"The Mythical Man Month\"";
            var expectedPageCount1 = $"\"{book1.PageCount}\"";
            var expectedTitle2 = "\"What it is to be a \"\"coder\"\"\"";
            var expectedPageCount2 = $"\"{book2.PageCount}\"";

            var books = new List<Book> { book1, book2 };

            //ACT
            var result = books.ToFileStreamResult(options =>
            {
                options.UseQuotedIdentifiers = true;
                options.UsePropertyNamesAsHeaders = false;
            });

            //ASSERT
            var actualBytes = new Byte[result.FileStream.Length];
            result.FileStream.Read(actualBytes, 0, actualBytes.Length);

            var actualLines = Encoding.UTF8.GetString(actualBytes).Split("\r\n");

            var actualLineData1 = actualLines.First().Split(",");
            Assert.AreEqual(2, actualLineData1.Length);
            Assert.AreEqual(expectedTitle1, actualLineData1.First());
            Assert.AreEqual(expectedPageCount1, actualLineData1.Last());

            var actualLineData2 = actualLines.Last().Split(",");
            Assert.AreEqual(2, actualLineData2.Length);
            Assert.AreEqual(expectedTitle2, actualLineData2.First());
            Assert.AreEqual(expectedPageCount2, actualLineData2.Last());
        }


        [TestMethod]
        public void Given_OptionsInstance_When_ToFileStreamResultIsPassedInstance_Then_ProvidedOptionsAreUsed()
        {
            var book1 = new Book() { Title = "The Mythical Man Month", PageCount = 322 };
            var book2 = new Book() { Title = "What it is to be a \"coder\"", PageCount = 0 };
            var books = new List<Book> { book1, book2 };

            var options = new EnumerableExtension.Options()
            {
                Delimiter = ",",
                EndOfLine = "\r\n",
                FileDownloadName = "library.csv",
                UsePropertyNamesAsHeaders = true,
                UseQuotedIdentifiers = true
            };

            books.ToFileStreamResult(options);
        }

        [TestMethod]
        public void
            Given_HeadersUsingPascalCasedPropertyNames_When_AddSpaceBetweenWordsIsEnabled_Then_PropertyNamesAreSpaced()
        {
            //ARRANGE
            var data = new List<Book>();

            //ACT
            var actuals = data.ToFileStreamResult(options => {
                options.UsePropertyNamesAsHeaders = true;
                options.AddSpacesToPropertyNameBasedHeaders = true;
            });

            //ASSERT

            var actualBytes = new Byte[actuals.FileStream.Length];
            actuals.FileStream.Read(actualBytes, 0, actualBytes.Length);

            var headerRow = Encoding.UTF8.GetString(actualBytes).Split("\r\n")[0].Split(",");

            Assert.IsTrue(headerRow.Contains("Title"));
            Assert.IsTrue(headerRow.Contains("Page Count"));
        }

        [TestMethod]
        public void Given_ListOfExpandoObjects_When_ConvertedToFileStream_Then_ExpandoObjectsKeyValuePairsAreOutput()
        {
            dynamic d = new ExpandoObject();
            d.ColumnA = "Egg";

            var data = new List<ExpandoObject> { d };

            var actuals = data.ToFileStreamResult();


            var actualBytes = new Byte[actuals.FileStream.Length];
            actuals.FileStream.Read(actualBytes, 0, actualBytes.Length);

            var actualLines = Encoding.UTF8.GetString(actualBytes).Split("\r\n");
            var headerRow = actualLines.First().Split(",");
            var dataRow = actualLines.Skip(1).First().Split(",");

            Assert.IsTrue(headerRow.First() == "ColumnA");
            Assert.IsTrue(dataRow.First() == "Egg");
        }


        [TestMethod]
        public void Given_ListOfDictionaryObjects_When_ConvertedToFileStream_Then_DictionaryObjectsKeyValuePairsAreOutput()
        {
            var row1 = new Dictionary<string, object>();
            row1.Add("ColumnA", "Egg");
            row1.Add("ColumnB", "Bacon");

            var data = new List<Dictionary<string, object>>();
            data.Add(row1);

            var actuals = data.ToFileStreamResult();

            var actualBytes = new Byte[actuals.FileStream.Length];
            actuals.FileStream.Read(actualBytes, 0, actualBytes.Length);

            var actualLines = Encoding.UTF8.GetString(actualBytes).Split("\r\n");
            var headerRow = actualLines.First().Split(",");
            var dataRow = actualLines.Skip(1).First().Split(",");

            Assert.IsTrue(headerRow.First() == "ColumnA");
            Assert.IsTrue(dataRow.First() == "Egg");
        }

        [TestMethod]
        public void Given_ListOfKeyValuePairs_When_ConvertedToFileStream_Then_KeyValuePairsAreOutput()
        {
            KeyValuePair<string, object> row1ColumnA = new KeyValuePair<string, object>("ColumnA", "Egg");
            KeyValuePair<string, object> row1ColumnB = new KeyValuePair<string, object>("ColumnB", "Bacon");

            var row1 = new List<KeyValuePair<string, object>>();
            row1.Add(row1ColumnA);
            row1.Add(row1ColumnB);

            var data = new List<List<KeyValuePair<string, object>>>();
            data.Add(row1);

            var actuals = data.ToFileStreamResult();

            var actualBytes = new Byte[actuals.FileStream.Length];
            actuals.FileStream.Read(actualBytes, 0, actualBytes.Length);

            var actualLines = Encoding.UTF8.GetString(actualBytes).Split("\r\n");
            var headerRow = actualLines.First().Split(",");
            var dataRow = actualLines.Skip(1).First().Split(",");

            Assert.IsTrue(headerRow.First() == "ColumnA");
            Assert.IsTrue(dataRow.First() == "Egg");
        }
    }
}
