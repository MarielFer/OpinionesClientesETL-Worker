using OpinionesClientesETL.Worker;
using OpinionesClientesETL.Domain.Interfaces;
using OpinionesClientesETL.Infrastructure.Extractors;
using OpinionesClientesETL.Infrastructure.Staging;
using System.IO;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

var extractionSource = builder.Configuration["Extraction:Source"] ?? "Csv";

var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? throw new InvalidOperationException("No se configuro la URL de la API.");

builder.Services.AddHttpClient<ApiCommentExtractor>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
});

builder.Services.AddTransient<IExtractor>(serviceProvider =>
{
    if (extractionSource.Equals("Sql", StringComparison.OrdinalIgnoreCase))
    {
        var connectionString = builder.Configuration["SqlServer:ConnectionString"]
            ?? throw new InvalidOperationException("No se configuro la conexion SQL Server.");

        return new SqlOpinionsExtractor(connectionString);
    }

    if (extractionSource.Equals("Api", StringComparison.OrdinalIgnoreCase))
    {
        return serviceProvider.GetRequiredService<ApiCommentExtractor>();
    }

    var csvPath = builder.Configuration["Csv:SurveyFilePath"]
        ?? throw new InvalidOperationException("No se configuro la ruta del CSV.");

    return new CsvSurveyExtractor(
        Path.Combine(builder.Environment.ContentRootPath, csvPath));
});

var stagingDirectory = builder.Configuration["Staging:Directory"] ?? "Staging";
builder.Services.AddSingleton<IStagingWriter>(_ => new JsonFileStagingWriter(Path.Combine(builder.Environment.ContentRootPath, stagingDirectory)));
builder.Services.AddHostedService<Worker>();

builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration.MinimumLevel.Information().WriteTo.Console().WriteTo.File("Logs/etl-.log", rollingInterval: RollingInterval.Day));

var host = builder.Build();
host.Run();


