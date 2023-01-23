<img max-width="500px" src="./data/icons/logo.svg">

## PdfToSvg
Library for converting pdf documents to svg. The core of the project is [itext7](https://github.com/itext/itext7-dotnet) from which the PDF creation functionality and other unnecessary functions have been removed. Also included the sourse SVG.NET and zlib.

### Pdf2Svg is based on:
1) iText 7 Community for .NET
2) SVG.NET
3) Zlib

### How to use:

```
using IText.Kernel.Pdf;
using RSB.ITextPDF.Pdf2Svg;
```

```
var fileInput = @"{input file}";
var dirOutput = @"{output file}";

if(!Directory.Exists(dirOutput))
    Directory.CreateDirectory(dirOutput);

using (var ms = new FileStream(fileInput, FileMode.Open))
using (var r = new PdfReader(ms))
using (var d = new PdfDocument(r))
{
    var index = 1;
    foreach (var page in new PdfToSvg().Process(d))
    {
        var svg = page.Canvas;
        var file = Path.Combine(dirOutput, $"{index}.svg");
        using (var output = new FileStream(file, FileMode.OpenOrCreate))
        {
            svg.Write(output);
        }
    }
}
```