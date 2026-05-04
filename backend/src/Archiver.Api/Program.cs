using Archiver.Application.Services;
using Archiver.Application.Compression;
using Archiver.Application.Abstractions;
using Archiver.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;

const string defaultUrl = "http://localhost:8080";
var url = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? defaultUrl;

Console.WriteLine($"Starting Archiver.Api on {url}");

var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.WebHost.UseKestrelCore();
builder.WebHost.UseUrls(url);

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = ArchiveDefaults.MaxFileSizeBytes + 1024 * 1024;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = ArchiveDefaults.MaxFileSizeBytes + 1024 * 1024;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders(
                "Content-Disposition",
                "X-Original-Size",
                "X-Result-Size",
                "X-Compression-Ratio",
                "X-Max-Code-Length",
                "X-Password-Protected",
                "X-File-Name");
    });
});

builder.Services.AddControllers();
builder.Services.AddScoped<ArchiveService>();
builder.Services.AddSingleton<IArchiveCodec, HuffmanArchiveCodec>();

var app = builder.Build();

app.UseCors("frontend");
app.MapControllers();

app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine($"Archiver.Api is listening on {url}");
});

app.Run();
