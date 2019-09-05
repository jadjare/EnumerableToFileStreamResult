using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ToFileStreamResult
{
    public static class EnumerableExtension
    {
        public static FileStreamResult ToFileStreamResult<T>(this IEnumerable<T> instance) =>
            instance.ToFileStreamResult(options: Options.UseStandard());

        public static FileStreamResult ToFileStreamResult<T>(this IEnumerable<T> instance, Action<Options> config)
        {
            var options = Options.UseStandard();
            config?.Invoke(options);

            return instance.ToFileStreamResult(options);
        }

        public static FileStreamResult ToFileStreamResult<T>(this IEnumerable<T> data, Options options)
        {
            if (options == null) options = Options.UseStandard();

            var memoryStream = new MemoryStream();
            var dataAsEnumerableKeyValuePairs = data as IEnumerable<IEnumerable<KeyValuePair<string, object>>>;

            var headerNames = new string[] { } as IEnumerable<string>;

            if (options.UsePropertyNamesAsHeaders)
            {
                headerNames = dataAsEnumerableKeyValuePairs != null
                    ? GetHeaderNamesFromFirstRowKeys(dataAsEnumerableKeyValuePairs, options.AddSpacesToPropertyNameBasedHeaders)
                    : GetHeaderNamesFromProperties(typeof(T), options.AddSpacesToPropertyNameBasedHeaders);
            }

            if (headerNames.Any()) WriteFileHeader(memoryStream, headerNames, options);

            if (dataAsEnumerableKeyValuePairs != null)
            {
                WriteFileContentsForKeyValuePairBasedData(memoryStream, dataAsEnumerableKeyValuePairs, options);
            }
            else
            {
                WriteFileContents(memoryStream, data, options);
            }


            memoryStream.Position = 0;
            return new FileStreamResult(memoryStream, options.ContentType)
            {
                FileDownloadName = options.FileDownloadName
            };
        }


        private static IEnumerable<string> GetHeaderNamesFromProperties(Type type, bool addSpacesBetweenWords)
        {
            var headerNames = type.GetProperties().Select(prop =>
                addSpacesBetweenWords ? Regex.Replace(prop.Name, "([A-Z])", " $1").Trim() : prop.Name);

            return headerNames;
        }

        private static IEnumerable<string> GetHeaderNamesFromFirstRowKeys(IEnumerable<IEnumerable<KeyValuePair<string, object>>> data, bool addSpacesBetweenWords)
        {
            if (!data.Any()) return new string[] { };

            var headerNames = data.First().Select(x =>
                addSpacesBetweenWords ? Regex.Replace(x.Key, "([A-Z])", " $1").Trim() : x.Key);

            return headerNames;
        }

        private static void WriteFileHeader(MemoryStream memoryStream, IEnumerable<string> headerNames, Options options)
        {
            foreach (var headerName in headerNames)
            {
                var delimiterIsRequired = memoryStream.Length > 0;
                if (delimiterIsRequired) memoryStream.Write(Encoding.UTF8.GetBytes(options.Delimiter), 0, Encoding.UTF8.GetBytes(options.Delimiter).Length);

                if (options.UseQuotedIdentifiers) memoryStream.WriteByte((byte)'"');
                var headerBytes = Encoding.UTF8.GetBytes(headerName);
                memoryStream.Write(headerBytes, 0, headerBytes.Length);
                if (options.UseQuotedIdentifiers) memoryStream.WriteByte((byte)'"');
            }
        }

        private static void WriteFileContents<T>(MemoryStream memoryStream, IEnumerable<T> data, Options options)
        {
            var endOfLineBytes = Encoding.UTF8.GetBytes(options.EndOfLine);
            var delimiterBytes = Encoding.UTF8.GetBytes(options.Delimiter);

            var propertiesOfT = typeof(T).GetProperties();
            var lastProp = propertiesOfT.Last();
            foreach (var item in data)
            {
                var newLineRequired = memoryStream.Length > 0;
                if (newLineRequired) memoryStream.Write(endOfLineBytes, 0, endOfLineBytes.Length);

                foreach (var propertyInfo in propertiesOfT)
                {
                    var propValue = propertyInfo.GetValue(item);
                    var itemValue = propValue == null ? "" : propValue.ToString();
                    if (options.UseQuotedIdentifiers) itemValue = $"\"{itemValue.Replace("\"", "\"\"")}\"";

                    var itemBytes = Encoding.UTF8.GetBytes(itemValue);
                    memoryStream.Write(itemBytes, 0, itemBytes.Length);

                    var isEndOfLine = propertyInfo == lastProp;
                    if (!isEndOfLine) memoryStream.Write(delimiterBytes, 0, delimiterBytes.Length);
                }
            }
        }

        private static void WriteFileContentsForKeyValuePairBasedData(MemoryStream memoryStream, IEnumerable<IEnumerable<KeyValuePair<string, object>>> data, Options options)
        {
            var endOfLineBytes = Encoding.UTF8.GetBytes(options.EndOfLine);
            var delimiterBytes = Encoding.UTF8.GetBytes(options.Delimiter);

            var dataKeys = data.First().Select(x => x.Key);
            var lastKey = dataKeys.Last();
            foreach (var item in data)
            {
                var newLineRequired = memoryStream.Length > 0;
                if (newLineRequired) memoryStream.Write(endOfLineBytes, 0, endOfLineBytes.Length);

                foreach (var key in dataKeys)
                {
                    var propValue = item.First(x => x.Key == key).Value;
                    var itemValue = propValue == null ? "" : propValue.ToString();
                    if (options.UseQuotedIdentifiers) itemValue = $"\"{itemValue.Replace("\"", "\"\"")}\"";

                    var itemBytes = Encoding.UTF8.GetBytes(itemValue);
                    memoryStream.Write(itemBytes, 0, itemBytes.Length);

                    var isEndOfLine = key == lastKey;
                    if (!isEndOfLine) memoryStream.Write(delimiterBytes, 0, delimiterBytes.Length);
                }
            }
        }

        public class Options
        {
            private string _delimiter = "";
            private string _endOfLine = "";
            private string _contentType = "application/octet-stream";

            public static Options UseStandard() => new Options
            {
                ContentType = "application/octet-stream",
                Delimiter = ",",
                UsePropertyNamesAsHeaders = true,
                AddSpacesToPropertyNameBasedHeaders = false,
                EndOfLine = "\r\n",
                FileDownloadName = ""
            };

            public string ContentType
            {
                get => _contentType;
                set
                {
                    if (value.Split("/").Length != 2) throw new ArgumentNullException(nameof(ContentType), "Content type should be a 2 part identifier, separated by a / character, e.g. 'ApplicationException/octet-Stream'. These are also known as a MIME Type or Media Type");
                    _contentType = value;
                }
            }

            public string Delimiter
            {
                get => _delimiter;
                set => _delimiter = value ?? throw new ArgumentNullException(nameof(Delimiter));
            }

            public string EndOfLine
            {
                get => _endOfLine;
                set => _endOfLine = value ?? throw new ArgumentNullException(nameof(EndOfLine));
            }

            public string FileDownloadName { get; set; }
            public bool UsePropertyNamesAsHeaders { get; set; }
            public bool AddSpacesToPropertyNameBasedHeaders { get; set; }

            public bool UseQuotedIdentifiers { get; set; }
        }
    }
}
