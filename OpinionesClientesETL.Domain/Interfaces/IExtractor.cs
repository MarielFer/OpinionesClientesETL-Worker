
using OpinionesClientesETL.Domain.Entities;

namespace OpinionesClientesETL.Domain.Interfaces;

public interface IExtractor
{
    Task<IEnumerable<ExtractedRecord>> ExtractAsync(
        CancellationToken cancellationToken = default);
}
