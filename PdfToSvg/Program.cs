// See https://aka.ms/new-console-template for more information
using IText.Kernel.Pdf;
using RSB.ITextPDF.Pdf2Svg;

var fileInput = @"C:";
var dirOutput = @"C:";

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