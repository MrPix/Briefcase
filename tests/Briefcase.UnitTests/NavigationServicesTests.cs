using System.Net;
using Briefcase.ApiService.Services;

namespace Briefcase.UnitTests;

[TestClass]
public sealed class NavigationServicesTests
{
    [TestMethod]
    public void Catalog_BuildsAllSelectedTargetsInCatalogOrder()
    {
        var catalog = new NavigationApplicationCatalog();

        var targets = catalog.BuildTargets(50.4501, 30.5234, ["maps-me", "google-maps", "waze", "locus-map"]);

        CollectionAssert.AreEqual(
            new[] { "google-maps", "waze", "locus-map", "maps-me" },
            targets.Select(target => target.ApplicationId).ToArray());
        Assert.IsTrue(targets[0].Uri.Contains("destination=50.4501%2C30.5234"));
        Assert.IsTrue(targets[1].Uri.Contains("ll=50.4501%2C30.5234"));
        Assert.IsTrue(targets[2].Uri.StartsWith("intent:#Intent;action=locus.api.android.ACTION_NAVIGATION_START"));
        Assert.AreEqual("mapsme://map?ll=50.4501%2C30.5234", targets[3].Uri);
    }

    [TestMethod]
    public async Task Resolver_UsesPlaceDataInsteadOfCameraCoordinates()
    {
        var resolver = new GoogleMapsResolver(new HttpClient(new StubHandler(_ => throw new AssertFailedException("HTTP should not be called."))));

        var result = await resolver.ResolveAsync(
            "https://www.google.com/maps/place/Test/@50.4883905,30.4056997,13z/data=!3d50.296784!4d30.5328013");

        Assert.AreEqual(MapResolutionOutcome.Success, result.Outcome);
        Assert.AreEqual(50.296784, result.Latitude);
        Assert.AreEqual(30.5328013, result.Longitude);
    }

    [TestMethod]
    public async Task Resolver_FollowsAllowedRedirectAndReadsMetadata()
    {
        var handler = new StubHandler(request => request.RequestUri!.Host == "maps.app.goo.gl"
            ? new HttpResponseMessage(HttpStatusCode.Redirect)
            {
                Headers = { Location = new Uri("https://maps.google.com/?q=Place") }
            }
            : new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"latitude\":50.4501,\"longitude\":30.5234}")
            });
        var resolver = new GoogleMapsResolver(new HttpClient(handler));

        var result = await resolver.ResolveAsync("https://maps.app.goo.gl/example");

        Assert.AreEqual(MapResolutionOutcome.Success, result.Outcome);
        Assert.AreEqual(50.4501, result.Latitude);
        Assert.AreEqual(30.5234, result.Longitude);
    }

    [TestMethod]
    public async Task Resolver_ReadsDestinationFromPlacePreviewInsteadOfInitializationStateCamera()
    {
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath == "/maps/preview/place"
            ? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(")]}'\n[[[2548.7,30.5328013,50.296784]],null,[null,null,50.296784,30.5328013],\"0x40d4b9940f626f2d:0x7bc458f3b64e5663\"]")
            }
            : new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<link href=\"/maps/preview/place?authuser=0&amp;q=Destination\"><script>APP_INITIALIZATION_STATE=[[[20307.68,30.4056997,50.4883905]]]</script>")
            });
        var resolver = new GoogleMapsResolver(new HttpClient(handler));

        var result = await resolver.ResolveAsync("https://maps.google.com/?q=Place&ftid=example");

        Assert.AreEqual(MapResolutionOutcome.Success, result.Outcome);
        Assert.AreEqual(50.296784, result.Latitude);
        Assert.AreEqual(30.5328013, result.Longitude);
    }

    [TestMethod]
    public async Task Resolver_RejectsRedirectOutsideGoogle()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Redirect)
        {
            Headers = { Location = new Uri("https://example.com/location") }
        });
        var resolver = new GoogleMapsResolver(new HttpClient(handler));

        var result = await resolver.ResolveAsync("https://maps.app.goo.gl/example");

        Assert.AreEqual(MapResolutionOutcome.Failed, result.Outcome);
    }

    [TestMethod]
    public async Task Resolver_IgnoresNonGoogleUrl()
    {
        var resolver = new GoogleMapsResolver(new HttpClient(new StubHandler(_ => throw new AssertFailedException("HTTP should not be called."))));

        var result = await resolver.ResolveAsync("https://example.com/@50.4501,30.5234");

        Assert.AreEqual(MapResolutionOutcome.NotApplicable, result.Outcome);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(send(request));
    }
}