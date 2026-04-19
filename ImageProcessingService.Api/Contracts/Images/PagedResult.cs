namespace ImageProcessingService.Api.Contracts.Images;

public sealed class PagedResult<T>
{
    public required IReadOnlyCollection<T> Items { get; init; }

    public required int Page { get; init; }

    public required int Limit { get; init; }

    public required int TotalCount { get; init; }
}
