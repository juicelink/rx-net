using RxLibrary;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddGrpcClient<GrpcService.Protos.MyService.MyServiceClient>(o =>
{
    o.Address = new Uri("http://localhost:5000");
});
builder.Services.AddTransient<GrpcClient>();

var app = builder
    .Build();

app.Urls.Add("http://*:5050");

app.MapControllers();

app.Run();
