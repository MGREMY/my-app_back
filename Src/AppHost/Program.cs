using AppHost.Extension;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedisContainer();
var postgres = builder.AddPostgresContainer();
var keycloak = builder.AddKeycloakContainer();
var mailPit = builder.AddMailPitContainer();

builder.AddProject<MyApp_Host_Api>("MyApp")
    .WithUrl("/scalar", "Scalar documentation")
    .WithExternalHttpEndpoints()
    .WaitFor(redis)
    .WaitFor(postgres)
    .WaitForStart(keycloak)
    .WaitForStart(mailPit)
    .WithReference(redis)
    .WithReference(postgres)
    .WithReference(keycloak)
    .WithReference(mailPit)
    .WithEnvironment("AuthDomain", $"{builder.Configuration["api_config:auth:domain"]}/realms/myApp")
    .WithEnvironment("AuthAudience", builder.Configuration["api_config:auth:audience"])
    .WithEnvironment("AuthOrigins", builder.Configuration["api_config:auth:origins"])
    .WithEnvironment("DataPath", builder.Configuration["api_config:data_path"]);

builder.Build().Run();