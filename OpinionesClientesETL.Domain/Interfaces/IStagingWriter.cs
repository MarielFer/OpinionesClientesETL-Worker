using OpinionesClientesETL.Domain.Entities;

namespace OpinionesClientesETL.Domain.Interfaces;

public interface IStagingWriter
{
    Task WriteAsync(
        IEnumerable<ExtractedRecord> records,
        string source,
        CancellationToken cancellationToken = default);
}
