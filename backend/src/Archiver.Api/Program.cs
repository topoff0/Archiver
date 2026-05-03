using Archiver.Application.Abstractions;
using Archiver.Application.Services;
using Archiver.Application;
using Archiver.Application.Compression;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.WebHost.UseKestrelCore();
builder.WebHost.UseUrls(Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:5000");

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

app.Run();
