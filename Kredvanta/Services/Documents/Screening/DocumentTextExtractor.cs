using System.Text;
using Tesseract;
using UglyToad.PdfPig;

namespace Kredvanta.Services.Documents.Screening;

public record ExtractionResult(bool Succeeded, string Text, string? Reason = null)
{
    public static ExtractionResult Ok(string text) => new(true, text);
    public static ExtractionResult Unavailable(string reason) => new(false, "", reason);
}

public interface IDocumentTextExtractor
{
    Task<ExtractionResult> ExtractAsync(Stream content, string extension, CancellationToken ct = default);
}

/// <summary>
/// PDFs with a text layer are read with PdfPig (no OCR needed).
/// JPG/PNG images are read with Tesseract OCR (needs Screening:TessdataPath).
/// Scanned (image-only) PDFs are NOT handled yet: they return Unavailable and are flagged for an officer.
/// Extracted text is used in memory only and is never stored.
/// </summary>
public class DocumentTextExtractor : IDocumentTextExtractor
{
    private const int MaxPdfPages = 20;
    private const int MinimumUsefulLength = 20;

    private readonly string? _tessdataPath;
    private readonly string _language;
    private readonly ILogger<DocumentTextExtractor> _log;

    public DocumentTextExtractor(IConfiguration config, ILogger<DocumentTextExtractor> log)
    {
        _tessdataPath = config["Screening:TessdataPath"];
        _language = config["Screening:OcrLanguage"] ?? "eng";
        _log = log;
    }

    public async Task<ExtractionResult> ExtractAsync(Stream content, string extension,
        CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        return extension.ToLowerInvariant() switch
        {
            ".pdf" => ExtractPdf(bytes),
            ".png" or ".jpg" or ".jpeg" => ExtractImage(bytes),
            _ => ExtractionResult.Unavailable("Unsupported file type.")
        };
    }

    private ExtractionResult ExtractPdf(byte[] bytes)
    {
        try
        {
            using var pdf = PdfDocument.Open(bytes);
            var sb = new StringBuilder();
            foreach (var page in pdf.GetPages().Take(MaxPdfPages))
                sb.AppendLine(page.Text);

            var text = sb.ToString();
            return text.Trim().Length < MinimumUsefulLength
                ? ExtractionResult.Unavailable("The PDF has no readable text layer (it is probably a scan).")
                : ExtractionResult.Ok(text);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "PDF text extraction failed.");
            return ExtractionResult.Unavailable("The PDF could not be read (it may be damaged or password protected).");
        }
    }

    private ExtractionResult ExtractImage(byte[] bytes)
    {
        if (string.IsNullOrWhiteSpace(_tessdataPath) || !Directory.Exists(_tessdataPath))
            return ExtractionResult.Unavailable("OCR is not configured on the server.");

        try
        {
            // TesseractEngine is not thread-safe; the worker is single-reader, and we create one per call.
            using var engine = new TesseractEngine(_tessdataPath, _language, EngineMode.Default);
            using var image = Pix.LoadFromMemory(bytes);
            using var page = engine.Process(image);
            var text = page.GetText() ?? "";

            return text.Trim().Length < MinimumUsefulLength
                ? ExtractionResult.Unavailable("Text could not be read clearly from the image.")
                : ExtractionResult.Ok(text);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "OCR failed.");
            return ExtractionResult.Unavailable("The image could not be read.");
        }
    }
}