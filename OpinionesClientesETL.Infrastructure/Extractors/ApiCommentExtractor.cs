using System.Net.Http.Json;
using OpinionesClientesETL.Domain.Entities;
using OpinionesClientesETL.Domain.Interfaces;

namespace OpinionesClientesETL.Infrastructure.Extractors;

public class ApiCommentExtractor : IExtractor
{
    private readonly HttpClient _httpClient;

    public ApiCommentExtractor(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<ExtractedRecord>> ExtractAsync(CancellationToken cancellationToken = default)
    {
        var comments = await _httpClient.GetFromJsonAsync<List<ApiComment>>("comments", cancellationToken);

        if (comments is null)
        {
            return Enumerable.Empty<ExtractedRecord>();
        }

        return comments.Select(comment => new ExtractedRecord
        {
            Source = "API REST - JSONPlaceholder",
            ExternalId = comment.Id.ToString(),
            Content = comment.Body,
            Author = comment.Email
        }).ToList();
    }
}
