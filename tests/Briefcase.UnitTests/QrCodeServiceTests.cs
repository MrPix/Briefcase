using Briefcase.ApiService.Services;

namespace Briefcase.UnitTests;

[TestClass]
public sealed class QrCodeServiceTests
{
    private static readonly byte[] PngMagicBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [TestMethod]
    public void GeneratePng_ReturnsNonEmptyByteArray()
    {
        var svc = new QrCodeService();
        var png = svc.GeneratePng("https://example.com");
        Assert.IsTrue(png.Length > 0);
    }

    [TestMethod]
    public void GeneratePng_ReturnsByteArrayStartingWithPngSignature()
    {
        var svc = new QrCodeService();
        var png = svc.GeneratePng("test-payload");
        CollectionAssert.IsSubsetOf(PngMagicBytes, png.Take(8).ToArray());
    }

    [TestMethod]
    public void GeneratePng_DifferentPayloads_ProduceDifferentImages()
    {
        var svc = new QrCodeService();
        var png1 = svc.GeneratePng("payload-one");
        var png2 = svc.GeneratePng("payload-two");
        Assert.IsFalse(png1.SequenceEqual(png2));
    }

    [TestMethod]
    public void GeneratePng_SamePayload_ProducesSameSizeImage()
    {
        var svc = new QrCodeService();
        var png1 = svc.GeneratePng("stable-payload");
        var png2 = svc.GeneratePng("stable-payload");
        Assert.AreEqual(png1.Length, png2.Length);
    }

    [TestMethod]
    public void GeneratePng_LargerPixelsPerModule_ProducesLargerImage()
    {
        var svc = new QrCodeService();
        var smallPng = svc.GeneratePng("payload", pixelsPerModule: 5);
        var largePng = svc.GeneratePng("payload", pixelsPerModule: 20);
        Assert.IsTrue(largePng.Length > smallPng.Length);
    }
}
