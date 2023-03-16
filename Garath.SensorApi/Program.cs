using Garath.SensorApi;
using MediatR;
using Microsoft.AspNetCore.ResponseCompression;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configure host
builder.Host.UseSystemd();

// Add services to the container.

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddScoped<PgSensorDataProvider>();
builder.Services.Configure<PgSensorDataProviderConfiguration>(
    config => config.ConnectionString = builder.Configuration.GetConnectionString("SensorDatabase")
);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "ApiRelaxed",
        builder =>
        {
            builder.AllowAnyOrigin();
            builder.WithMethods("GET");
        });
});

builder.Services.AddMediatR(Assembly.GetExecutingAssembly());
builder.Services.AddSignalR();
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpLogging(options =>
        options.LoggingFields =
            Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestMethod |
            Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestPath |
            Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponseStatusCode);
}

builder.Services.AddControllers();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseHttpLogging();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors();
app.UseAuthorization();
app.MapRazorPages();
app.MapControllers();
app.MapHub<SensorHub>("/SensorHub");
app.MapFallbackToFile("index.html");

app.Run();
