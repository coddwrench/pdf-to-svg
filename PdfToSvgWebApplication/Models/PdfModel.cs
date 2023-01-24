using System.Xml.Linq;

namespace PdfToSvgWebApp.Models
{
    public class PdfPageModel
    {
        public XElement? Value { get; set; }
        public string? PageSize { get; set; }
        public string? Orientation { get; set; }
    }

    public class PdfModel
    {
        public IEnumerable<PdfPageModel>? Pages { get; set; }
        public string? FileName { get; set; }
    }
}
