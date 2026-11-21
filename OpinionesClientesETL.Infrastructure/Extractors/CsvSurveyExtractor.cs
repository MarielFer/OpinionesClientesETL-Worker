using System.Globalization;
using System.Text;
using CsvHelper;
using OpinionesClientesETL.Domain.Entities;
using OpinionesClientesETL.Domain.Interfaces;

namespace OpinionesClientesETL.Infrastructure.Extractors;

public class CsvSurveyExtractor : IExtractor
{
    private readonly string _filePath;

    public CsvSurveyExtractor(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<IEnumerable<ExtractedRecord>> ExtractAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException(
                "No se encontrÃ³ el archivo CSV.", _filePath);
        }

        var records = new List<ExtractedRecord>();

        using var reader = new StreamReader(_filePath, Encoding.UTF8);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        if (!await csv.ReadAsync())
        {
            return records;
        }

        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dateText = csv.GetField(3);
            var ratingText = csv.GetField(6);

            records.Add(new ExtractedRecord
            {
                ExternalId = csv.GetField(0),
                CustomerId = csv.GetField(1),
                ProductId = csv.GetField(2),
                CreatedAt = DateTime.TryParse(
                    dateText, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date) ? date : null,
                Content = csv.GetField(4) ?? string.Empty,
                Classification = csv.GetField(5),
                Rating = decimal.TryParse(
                    ratingText, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out var rating)
                    ? rating : null,
                Source = csv.GetField(7) ?? string.Empty
            });
        }

        return records;
    }
}

