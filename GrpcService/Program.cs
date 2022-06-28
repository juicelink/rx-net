using GrpcService.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(k =>
{
    k.ConfigureEndpointDefaults(opt =>
    {
        opt.Protocols = HttpProtocols.Http2;
    });
    k.ListenAnyIP(5000);
});

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<MyService>();
app.MapGet("/healthcheck", () => "ok");

app.Run();
