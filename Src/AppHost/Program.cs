using AppHost.Extension;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedisContainer();
var postgres = builder.AddPostgresContainer();
var keycloak = builder.AddKeycloakContainer();
var mailPit = builder.AddMailPitContainer();

var authDomain = builder.Configuration["api_config:auth:domain"];
var authRealm = builder.Configuration["api_config:auth:realm"];
var corsOrigins = builder.Configuration.GetSection("api_config:auth:origins")
    .GetChildren()
    .Select(x => x.Value)
    .OfType<string>()
    .ToArray();

builder.AddProject<MyApp_Host_Api>("MyApp")
    .WithUrl("/scalar", "Scalar documentation")
    .WithExternalHttpEndpoints()
    .WaitFor(redis)
    .WaitFor(postgres)
    .WaitForStart(keycloak)
    .WaitForStart(mailPit)
    .WithReference(keycloak)
    .WithReference(mailPit)
    .WithEnvironment("Api__Database__ConnectionString", postgres)
    .WithEnvironment("Api__Cache__ConnectionString", redis)
    .WithEnvironment("Api__Auth__Domain", $"{authDomain}/realms/{authRealm}")
    .WithEnvironment("Api__Auth__Audience", builder.Configuration["api_config:auth:audience"])
    .WithEnvironment(context =>
    {
        for (var i = 0; i < corsOrigins.Length; i++)
            context.EnvironmentVariables[$"Api__Cors__Origins__{i}"] = corsOrigins[i];
    })
    .WithEnvironment("Api__Services__DataPath", builder.Configuration["api_config:data_path"]);

builder.Build().Run();