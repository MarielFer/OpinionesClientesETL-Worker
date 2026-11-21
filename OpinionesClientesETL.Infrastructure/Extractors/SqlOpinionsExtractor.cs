using Microsoft.Data.SqlClient;
using OpinionesClientesETL.Domain.Entities;
using OpinionesClientesETL.Domain.Interfaces;

namespace OpinionesClientesETL.Infrastructure.Extractors;

public class SqlOpinionsExtractor : IExtractor
{
    private readonly string _connectionString;

    public SqlOpinionsExtractor(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<ExtractedRecord>> ExtractAsync(
        CancellationToken cancellationToken = default)
    {
        const string query = """
            SELECT
                o.IdOpinion,
                o.IdCliente,
                o.IdProducto,
                o.IdFuente,
                o.Fecha,
                o.Comentario,
                c.Nombre AS Clasificacion,
                o.PuntajeSatisfaccion
            FROM Opiniones AS o
            LEFT JOIN Clasificaciones AS c
                ON o.IdClasificacion = c.IdClasificacion
            ORDER BY o.IdOpinion;
            """;

        var records = new List<ExtractedRecord>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new ExtractedRecord
            {
                ExternalId = reader["IdOpinion"].ToString(),
                CustomerId = reader["IdCliente"].ToString(),
                ProductId = reader["IdProducto"].ToString(),
                Source = reader["IdFuente"].ToString() ?? string.Empty,
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("Fecha")),
                Content = reader["Comentario"].ToString() ?? string.Empty,
                Classification = reader["Clasificacion"] == DBNull.Value
                    ? null
                    : reader["Clasificacion"].ToString(),
                Rating = Convert.ToDecimal(reader["PuntajeSatisfaccion"])
            });
        }

        return records;
    }
}