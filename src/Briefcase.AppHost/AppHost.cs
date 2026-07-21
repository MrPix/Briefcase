var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure resources — self-hosted Linux compatible (§9)
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("postgres-data")
    .AddDatabase("Briefcasedb");

var minio = builder.AddContainer("minio", "minio/minio", "latest")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "s3")
    .WithHttpEndpoint(port: 9001, targetPort: 9001, name: "console")
    .WithVolume("minio-data", "/data");

var redis = builder.AddRedis("redis");

var seq = builder.AddSeq("seq")
    .WithLifetime(ContainerLifetime.Persistent);

// API service — depends on PostgreSQL, MinIO, Redis (SignalR backplane), and Seq
var apiService = builder.AddProject<Projects.Briefcase_ApiService>("apiservice")
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(seq)
    .WithReference(minio.GetEndpoint("s3"))
    .WaitFor(postgres)
    .WaitFor(redis)
    .WithHttpHealthCheck("/health")
    .WithUrls(context => context.Urls.Add(new ResourceUrlAnnotation { Url = $"{context.Urls[0].Url}/scalar/v1", DisplayText = "Scalar" }));

// React (Vite) web frontend — lightweight SPA that talks to the API.
// Fixed port 5173 (not proxied) so the browser origin matches the API's
// Cors:AllowedOrigins entry for local development.
builder.AddNpmApp("webfrontend", "../Briefcase.React", "dev")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithEnvironment("VITE_API_BASE_URL", apiService.GetEndpoint("https"))
    .WithHttpEndpoint(env: "PORT", port: 5173, isProxied: false)
    .WithExternalHttpEndpoints();

builder.Build().Run();
