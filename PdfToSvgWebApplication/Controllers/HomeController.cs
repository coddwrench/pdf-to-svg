using System.Diagnostics;
using System.Xml.Linq;
using IText.Kernel.Geom;
using IText.Kernel.Pdf;
using ITextPdf2SVG;
using Microsoft.AspNetCore.Mvc;
using PdfToSvgWebApp.Models;

namespace PdfToSvgWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private static string PageSizeTpClass(PageSize ps)
        {
            return typeof(PageSize).GetFields().FirstOrDefault(_ => _.DeclaringType == typeof(PageSize) && ps.Equals(_.GetValue(null)))
                ?.Name.ToLower() ?? "unknown";
        }

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public IActionResult Upload(List<IFormFile> files)
        {
            var file = files.FirstOrDefault();

            if (file == null)
                return View("Index");

            using var fs = file.OpenReadStream();
            using var r = new PdfReader(fs);
            using var d = new PdfDocument(r);
            var pdfToSvg = new PdfToSvg();
            var pdf2SvgResult = pdfToSvg.Process(d);
            var pages = pdf2SvgResult.Select(_ =>
            {
                using var stream = new MemoryStream();
                using var reader = new StreamReader(stream);
                _.Canvas.Write(stream);
                stream.Position = 0;
                PageSizeTpClass(_.PageSize);
                return new PdfPageModel
                {
                    PageSize = PageSizeTpClass(_.PageSize),
                    Value = XElement.Load(reader),
                    Orientation = _.Size.Height > _.Size.Width ? "h" : "v"
                };
            }).ToList();
            return View("Index", new PdfModel
            {
                FileName = file.FileName,
                Pages = pages
            });
        }

        public IActionResult Index(PdfModel pdf)
        {
            return View();
        }
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}