using QRCoder;

namespace Briefcase.ApiService.Services;

/// <summary>
/// Generates QR code images from arbitrary string payloads (pairing tokens, transfer session IDs).
/// </summary>
public sealed class QrCodeService
{
    /// <summary>
    /// Returns a PNG byte array of a QR code encoding <paramref name="payload"/>.
    /// </summary>
    /// <param name="payload">The string to encode (e.g. a signed JWT or a session URL).</param>
    /// <param name="pixelsPerModule">Scale factor; defaults to 10 px per module (~330 px wide for typical payloads).</param>
    public byte[] GeneratePng(string payload, int pixelsPerModule = 10)
    {
        using var generator = new QRCodeGenerator();
        using QRCodeData data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var code = new PngByteQRCode(data);
        return code.GetGraphic(pixelsPerModule);
    }
}
