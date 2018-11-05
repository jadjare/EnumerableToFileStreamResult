using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

            var headerNames = new string[] { } as IEnumerable<string>;

            if (options.UsePropertyNamesAsHeaders) headerNames = typeof(T).GetProperties().Select(prop => prop.Name);

            if (headerNames.Any()) WriteFileHeader(memoryStream, headerNames, options);

            WriteFileContents<T>(memoryStream, data, options);

            memoryStream.Position = 0;
            return new FileStreamResult(memoryStream, options.ContentType)
            {
                FileDownloadName = options.FileDownloadName
            };
        }

        private static void WriteFileHeader(MemoryStream memoryStream, IEnumerable<string> headerNames, Options options)
        {
            foreach(var headerName in headerNames)
            {
                var delimiterIsRequired = memoryStream.Length > 0;
                if(delimiterIsRequired) memoryStream.Write(Encoding.UTF8.GetBytes(options.Delimiter));

                if(options.UseQuotedIdentifiers) memoryStream.WriteByte((byte)'"');
                memoryStream.Write(Encoding.UTF8.GetBytes(headerName));
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
                if(newLineRequired) memoryStream.Write(endOfLineBytes);

                foreach (var propertyInfo in propertiesOfT)
                {
                    var itemValue = propertyInfo.GetValue(item).ToString();
                    if (options.UseQuotedIdentifiers) itemValue = $"\"{itemValue.Replace("\"", "\"\"")}\"";

                    memoryStream.Write(Encoding.UTF8.GetBytes(itemValue));

                    var isEndOfLine = propertyInfo == lastProp;
                    if(!isEndOfLine) memoryStream.Write(delimiterBytes);
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
                EndOfLine = "\r\n",
                FileDownloadName = ""
            };

            public string ContentType
            {
                get => _contentType;
                set
                {
                    if(value.Split("/").Length!=2) throw new ArgumentNullException(nameof(ContentType), "Content type should be a 2 part identifier, separated by a / character, e.g. 'ApplicationException/octet-Stream'. These are also known as a MIME Type or Media Type");
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


            public bool UseQuotedIdentifiers { get; set; }
        }
    }
}
