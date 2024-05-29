var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.AspireApp13_ApiService>("apiservice");

var ps = builder.AddPostgres("ps");
var db = ps.AddDatabase("ps");

builder.AddProject<Projects.AspireApp13_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
