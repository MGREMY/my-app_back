using AppHost.Extension;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedisContainer();
var postgres = builder.AddPostgresContainer();
var keycloak = builder.AddKeycloakContainer();
var mailPit = builder.AddMailPitContainer();

var api = builder.AddApiContainer(postgres, redis)
    .WaitFor(redis)
    .WaitFor(postgres)
    .WaitForStart(keycloak)
    .WaitForStart(mailPit)
    .WithReference(keycloak)
    .WithReference(mailPit);

var gateway = builder.AddGatewayContainer(api)
    .WaitFor(api);

builder.Build().Run();