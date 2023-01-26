// See https://aka.ms/new-console-template for more information

using IText.Kernel.Pdf;
using ITextPdf2SVG;

namespace PdfToSvgConsoleApp;

class Program
{
    public static void Main(string[] args)
    {
        var fileInput = string.Empty;
        var dirOutput = string.Empty;
        var isDirOutputInArgs = false;
        if (args.Length > 0)
        {
            fileInput = args[0];
            fileInput = fileInput.Trim('"');
        }

        if (args.Length > 1)
        {
            dirOutput = args[1];
            dirOutput = dirOutput.Trim('"');
            isDirOutputInArgs = true;
        }

        if (string.IsNullOrWhiteSpace(fileInput))
        {
            fileInput = FileInputLoop();
        }

        if (!isDirOutputInArgs && string.IsNullOrWhiteSpace(dirOutput))
        {
            dirOutput = Path.GetDirectoryName(fileInput);
        }

        if (string.IsNullOrWhiteSpace(dirOutput) && !ValidateOutputDirectory(dirOutput))
        {
            Close();
            return;
        }

        dirOutput = Path.Combine(dirOutput, Path.GetFileNameWithoutExtension(fileInput));

        if (Directory.Exists(dirOutput) && Directory.EnumerateFiles(dirOutput).Any())
        {
            Console.WriteLine("directory {0} not empty", dirOutput);
            Close();
            return;
        }

        Directory.CreateDirectory(dirOutput);

        Process(fileInput, dirOutput);

        Close();
    }

    static bool ValidateOutputDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            Console.WriteLine("directory path is empty");
            return false;

        }

        if (Directory.Exists(directoryPath))
        {
            Console.WriteLine("file {0} not exist", directoryPath);
            return false;
        }

        return true;
    }

    static bool ValidatePdfFile(string? filePath)
    {

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Console.WriteLine("file path is empty");
            return false;
        }

        if (!File.Exists(filePath))
        {
            Console.WriteLine("file {0} not exist", filePath);
            return false;
        }

        if (Path.GetExtension(filePath) != ".pdf")
        {
            Console.WriteLine("file {0} not pdf", filePath);
            return false;
        }

        return true;
    }

    static string FileInputLoop()
    {
        while (true)
        {
            Console.Write("Pdf file: ");
            var filepath = Console.ReadLine();
            filepath = filepath.Trim('"');
            if (ValidatePdfFile(filepath))
                return filepath;
        }

    }

    static void Close()
    {
        Console.Write("the program is over press any key");
        Console.ReadKey();
    }

    static void Process(string s, string dirOutput1)
    {
        Console.Write("Start process file {0}", s);

        using (var ms = new FileStream(s, FileMode.Open))
        using (var r = new PdfReader(ms))
        using (var d = new PdfDocument(r))
        {
            var index = 1;
            foreach (var page in new PdfToSvg().Process(d))
            {
                var svg = page.Canvas;
                var file = Path.Combine(dirOutput1, $"{index}.svg");
                using var output = new FileStream(file, FileMode.OpenOrCreate);
                svg.Write(output);
            }
        }
        Console.Write("Finish output {0}", dirOutput1);
    }

}