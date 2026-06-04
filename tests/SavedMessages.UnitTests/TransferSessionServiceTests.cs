using SavedMessages.ApiService.Services;

namespace SavedMessages.UnitTests;

[TestClass]
public sealed class TransferSessionServiceTests
{
    [TestMethod]
    public void CreateSession_ReturnsNonEmptyId()
    {
        var svc = new TransferSessionService();
        var id = svc.CreateSession();
        Assert.IsFalse(string.IsNullOrEmpty(id));
    }

    [TestMethod]
    public void CreateSession_ReturnsUniqueIds()
    {
        var svc = new TransferSessionService();
        var id1 = svc.CreateSession();
        var id2 = svc.CreateSession();
        Assert.AreNotEqual(id1, id2);
    }

    [TestMethod]
    public void TryPush_ReturnsTrue_WhenSessionExistsAndNotClaimed()
    {
        var svc = new TransferSessionService();
        var id = svc.CreateSession();
        var result = svc.TryPush(id, "hello");
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void TryPush_ReturnsFalse_WhenSessionDoesNotExist()
    {
        var svc = new TransferSessionService();
        var result = svc.TryPush("nonexistent-session", "hello");
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryPush_ReturnsFalse_WhenSessionAlreadyClaimed()
    {
        var svc = new TransferSessionService();
        var id = svc.CreateSession();
        svc.TryPush(id, "first");
        var result = svc.TryPush(id, "second");
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryGet_ReturnsFalseFound_WhenSessionDoesNotExist()
    {
        var svc = new TransferSessionService();
        var (found, _, _) = svc.TryGet("unknown-id");
        Assert.IsFalse(found);
    }

    [TestMethod]
    public void TryGet_ReturnsTrueFound_AndNullContent_BeforePush()
    {
        var svc = new TransferSessionService();
        var id = svc.CreateSession();
        var (found, content, expired) = svc.TryGet(id);
        Assert.IsTrue(found);
        Assert.IsNull(content);
        Assert.IsFalse(expired);
    }

    [TestMethod]
    public void TryGet_ReturnsContent_AfterPush()
    {
        var svc = new TransferSessionService();
        var id = svc.CreateSession();
        svc.TryPush(id, "the-payload");
        var (found, content, expired) = svc.TryGet(id);
        Assert.IsTrue(found);
        Assert.AreEqual("the-payload", content);
        Assert.IsFalse(expired);
    }

    [TestMethod]
    public void TryGet_ReturnsExpiredFalse_ForFreshSession()
    {
        var svc = new TransferSessionService();
        var id = svc.CreateSession();
        var (_, _, expired) = svc.TryGet(id);
        Assert.IsFalse(expired);
    }
}
