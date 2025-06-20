using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace DocumentAnalyticsCloudApp.Services
{
    /*
     * TextExtractionService provides methods for extracting titles and full text content from uploaded PDF and Word (.docx) documents.
     * 
     *  -- Developed By: Ez-Aldeen Asqool (E.A Developer) --
     */
    public class TextExtractionService
    {
        public string ExtractTitleFromPdf(Stream pdfStream)
        {
            using var pdf = PdfDocument.Open(pdfStream);

            var metadataTitle = pdf.Information.Title;
            if (!string.IsNullOrWhiteSpace(metadataTitle))
                return metadataTitle.Trim();

            var firstPage = pdf.GetPage(1);

            var largestFontGroup = firstPage.Letters
                .Where(l => !string.IsNullOrWhiteSpace(l.Value)) 
                .GroupBy(l => l.FontSize)
                .OrderByDescending(g => g.Key) 
                .FirstOrDefault();

            if (largestFontGroup != null)
            {
                var title = string.Concat(largestFontGroup.Select(l => l.Value));
                return title.Trim();
            }

            return "Untitled PDF";
        }

        public string ExtractTitleFromWord(Stream wordStream)
        {
            using var wordDoc = WordprocessingDocument.Open(wordStream, false);
            var body = wordDoc.MainDocumentPart?.Document.Body;

            if (body == null) return "Untitled Word";

            var title = body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>()
                            .Select(p => p.InnerText)
                            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

            return title?.Trim() ?? "Untitled Word";
        }

        public string ExtractTitle(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLower();

            using var stream = file.OpenReadStream();

            if (extension == ".pdf")
                return ExtractTitleFromPdf(stream);
            else if (extension == ".docx")
                return ExtractTitleFromWord(stream);
            else
                return Path.GetFileNameWithoutExtension(file.FileName);
        }

        public string ExtractFullText(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLower();

            using var stream = file.OpenReadStream();

            if (extension == ".pdf")
            {
                using var pdf = PdfDocument.Open(stream);
                return string.Join("\n", pdf.GetPages().Select(p => p.Text));
            }
            else if (extension == ".docx")
            {
                using var doc = WordprocessingDocument.Open(stream, false);
                var body = doc.MainDocumentPart?.Document.Body;
                return body?.InnerText ?? "";
            }
            else
            {
                return "";
            }
        }

    }
}
