using System.Diagnostics;
using OpinionesClientesETL.Domain.Interfaces;

namespace OpinionesClientesETL.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IExtractor _extractor;
    private readonly IStagingWriter _stagingWriter;
    private readonly string _source;

    public Worker(
        ILogger<Worker> logger,
        IExtractor extractor,
        IStagingWriter stagingWriter,
        IConfiguration configuration)
    {
        _logger = logger;
        _extractor = extractor;
        _stagingWriter = stagingWriter;
        _source = configuration["Extraction:Source"] ?? "Unknown";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();
        int extractedCount = 0;
        int stagedCount = 0;
        int errorCount = 0;

        try
        {
            _logger.LogInformation("Iniciando proceso de extraccion desde {Source}...", _source);

            var records = (await _extractor.ExtractAsync(stoppingToken)).ToList();
            extractedCount = records.Count;

            await _stagingWriter.WriteAsync(records, _source, stoppingToken);
            stagedCount = records.Count;

            stopwatch.Stop();

            _logger.LogInformation("Extraccion finalizada. Registros obtenidos: {Total}", records.Count);
            _logger.LogInformation("Registros guardados en Staging. Fuente: {Source}", _source);
            _logger.LogInformation("Duracion total: {Duration} ms", stopwatch.ElapsedMilliseconds);

            foreach (var record in records.Take(3))
            {
                _logger.LogInformation(
                    "ID: {Id} | Cliente: {Customer} | Producto: {Product} | Clasificacion: {Classification}",
                    record.ExternalId, record.CustomerId, record.ProductId, record.Classification);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
            stopwatch.Stop();
            _logger.LogInformation("Proceso ETL cancelado. Duracion: {Duration} ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            errorCount = 1;
            stopwatch.Stop();
            _logger.LogError(ex, "Error ETL. Fuente: {Source} | Tipo: {ErrorType} | Mensaje: {ErrorMessage} | Duracion: {Duration} ms", _source, ex.GetType().Name, ex.Message, stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation("RESUMEN ETL | Fuente: {Source} | Extraidos: {Extracted} | Guardados en Staging: {Staged} | Errores: {Errors} | Duracion: {Duration} ms", _source, extractedCount, stagedCount, errorCount, stopwatch.ElapsedMilliseconds);
        }
    }
}
