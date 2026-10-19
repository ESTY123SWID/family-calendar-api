namespace my_project.Service.Common;

// The file types the system knows how to accept. For each one: the Content-Type the server sends back,
// and the first bytes ("magic number") a real file of that type starts with.
// The Content-Type is decided here, never taken from the client – a client could declare "text/html"
// for a file and have it served back as a web page
internal static class FileTypes
{
    // The longest signature check reads this many bytes (WebP: "RIFF" + 4 bytes size + "WEBP")
    public const int HeaderLength = 12;

    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] Pdf = "%PDF-"u8.ToArray();
    private static readonly byte[] Riff = "RIFF"u8.ToArray();
    private static readonly byte[] Webp = "WEBP"u8.ToArray();
    // A .docx file is a ZIP archive
    private static readonly byte[] Zip = [0x50, 0x4B, 0x03, 0x04];

    private static readonly Dictionary<string, FileType> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = new("image/jpeg", header => StartsWith(header, Jpeg)),
        [".jpeg"] = new("image/jpeg", header => StartsWith(header, Jpeg)),
        [".png"] = new("image/png", header => StartsWith(header, Png)),
        [".webp"] = new("image/webp", header => StartsWith(header, Riff) && header.Length >= 12 && header.Slice(8, 4).SequenceEqual(Webp)),
        [".pdf"] = new("application/pdf", header => StartsWith(header, Pdf)),
        [".docx"] = new("application/vnd.openxmlformats-officedocument.wordprocessingml.document", header => StartsWith(header, Zip))
    };

    // extension: lower case with the leading dot
    public static bool TryGet(string extension, out FileType fileType)
    {
        return Known.TryGetValue(extension, out fileType!);
    }

    private static bool StartsWith(ReadOnlySpan<byte> header, byte[] signature)
    {
        return header.StartsWith(signature);
    }

    public delegate bool SignatureCheck(ReadOnlySpan<byte> header);

    public sealed record FileType(string ContentType, SignatureCheck MatchesSignature);
}
