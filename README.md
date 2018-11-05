# EnumerableToFileStreamResult
A simple extension method designed to make it easy to convert an enumerable list of class objects to a FileStreamResult...  Or put another way quickly serve up the content of a list as a file download.

The FileStreamResult will contain a delimited data set.  By default a file is produced containing header rows, using the property names found on the class, and contents matching the value of each property, separated by commas.

*The solution is written in .NET CORE 2 using C#.*

# How to use
**TODO:** Complete this section properly

**TODO:** Publish as NuGet package

In short clone the solution and add a project reference to the `ToFileStreamResultExtensions` project.
At present there is actually only one file in the repository that is required for the extension method, so you could also simply copy and paste it's contents and use it as "seed work" in your own project.  The extension method code is found under `/src/ToFileStreamResultExtensions/EnumerableToFileStreamResultExtension.cs`

Once referenced you can follow one of the below examples to get started.

### Returning your own list of objects
```
[HttpGet]
public IActionResult Get()
{
  var book1 = new Book() { Title = "The Mythical Man Month", PageCount = 322 };
  var book2 = new Book() { Title = "What it is to be a 'coder'", PageCount = 0 };
  var books = new List<Book> { book1, book2};

  return books.ToFileStreamResult();
}
```

### A Dapper example
```
[HttpGet]
public IActionResult Get()
{
  var orderDetails = connection.Query<OrderDetail>(SELECT TOP 5 * FROM OrderDetails;).ToList();

  return orderDetails.ToFileStreamResult();
}
```

### Using Options
```
[HttpGet]
public IActionResult Get()
{
  var book1 = new Book() { Title = "The Mythical Man Month", PageCount = 322 };
  var book2 = new Book() { Title = "What it is to be a \"coder\"", PageCount = 0 };
  var books = new List<Book> { book1, book2};

  return books.ToFileStreamResult(options =>
    {
        options.UseQuotedIdentifiers = true;
        options.UsePropertyNamesAsHeaders = false;
        options.Delimiter = "\t";
        options.EndOfLine = "\r\n"
        options.ContentType = "application\ms-excel";
        options.FileDownloadName = "library-books.tab"
    });
}
```
