using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;

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

                    //if (options.UseQuotedIdentifiers) itemValue = '"' + itemValue.Replace("\"", "\"\"") + '"';
                    if (options.UseQuotedIdentifiers) itemValue = $"\"{itemValue.Replace("\"", "\"\"")}\"";

                    memoryStream.Write(Encoding.UTF8.GetBytes(itemValue));

                    var isEndOfLine = propertyInfo == lastProp;
                    if(!isEndOfLine) memoryStream.Write(delimiterBytes);
                }
            }
        }

        public class Options
        {
            public static Options UseStandard() => new Options
            {
                ContentType = "application/octet-stream",
                Delimiter = ",",
                UsePropertyNamesAsHeaders = true,
                EndOfLine = "\r\n",
                FileDownloadName = ""
            };

            public string ContentType { get; set; }
            public string Delimiter { get; set; }
            public string FileDownloadName { get; set; }
            public bool UsePropertyNamesAsHeaders { get; set; }
            public string EndOfLine { get; set; }
            public bool UseQuotedIdentifiers { get; set; }
        }
    }
}
