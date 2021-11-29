using Garath.SensorApi;

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

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpLogging(options =>
        options.LoggingFields =
            Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestMethod |
            Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestPath |
            Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponseStatusCode);
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.UseHttpLogging();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
