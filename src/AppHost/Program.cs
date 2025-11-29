using AppHost.Extension;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedisContainer();
var postgres = builder.AddPostgresContainer();
var keycloak = builder.AddKeycloakContainer();
var mailPit = builder.AddMailPitContainer();

builder.AddProject<Host_Api>(name: "MyApp")
    .WithUrl("/scalar", displayText: "Scalar documentation")
    .WithExternalHttpEndpoints()
    .WaitFor(redis)
    .WaitFor(postgres)
    .WaitForStart(keycloak)
    .WaitForStart(mailPit)
    .WithReference(redis)
    .WithReference(postgres)
    .WithReference(keycloak)
    .WithReference(mailPit)
    .WithEnvironment(name: "auth_domain", value: $"{builder.Configuration["api_config:auth:domain"]}/realms/myApp")
    .WithEnvironment(name: "auth_audience", value: builder.Configuration["api_config:auth:audience"])
    .WithEnvironment(name: "auth_origins", value: builder.Configuration["api_config:auth:origins"]);

builder.Build().Run();