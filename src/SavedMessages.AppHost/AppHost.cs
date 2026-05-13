var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure resources — self-hosted Linux compatible (§9)
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("postgres-data")
    .AddDatabase("savedmessagesdb");

var minio = builder.AddContainer("minio", "minio/minio", "latest")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "s3")
    .WithHttpEndpoint(port: 9001, targetPort: 9001, name: "console")
    .WithVolume("minio-data", "/data");

var redis = builder.AddRedis("redis");

var seq = builder.AddSeq("seq")
    .WithLifetime(ContainerLifetime.Persistent);

// API service — depends on PostgreSQL, MinIO, Redis (SignalR backplane), and Seq
var apiService = builder.AddProject<Projects.SavedMessages_ApiService>("apiservice")
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(seq)
    .WithReference(minio.GetEndpoint("s3"))
    .WaitFor(postgres)
    .WaitFor(redis)
    .WithHttpHealthCheck("/health");

// Web frontend — depends on the API service
builder.AddProject<Projects.SavedMessages_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
