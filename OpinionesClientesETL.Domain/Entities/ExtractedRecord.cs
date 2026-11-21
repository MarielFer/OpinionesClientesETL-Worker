namespace OpinionesClientesETL.Domain.Entities;

public class ExtractedRecord
{
    public string Source { get; set; } = string.Empty;

    public string? ExternalId { get; set; }

    public string Content { get; set; } = string.Empty;

    public string? Author { get; set; }

    public DateTime? CreatedAt { get; set; }

    public decimal? Rating { get; set; }

    public string? CustomerId { get; set; }

    public string? ProductId { get; set; }

    public string? Classification { get; set; }
}
