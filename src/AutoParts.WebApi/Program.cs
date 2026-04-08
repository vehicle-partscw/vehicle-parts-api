var builder = WebApplication.CreateBuilder(args);

// TODO: Register services (Application, Infrastructure, Auth, etc.)

var app = builder.Build();

// TODO: Configure middleware pipeline

app.MapGet("/", () => "AutoParts API is running.");

app.Run();
