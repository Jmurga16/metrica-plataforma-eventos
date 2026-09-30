using System.Text.Json;
using EventService.Application;
using Microsoft.Extensions.Caching.Distributed;

namespace EventService.Infrastructure;

public sealed class EventsCache(IDistributedCache cache) : IEventsCache
{
    private const string VersionKey = "events:version";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<(IReadOnlyList<EventDto>? Value, bool Hit)> GetAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        try
        {
            var key = await BuildKeyAsync(page, pageSize, cancellationToken);
            var json = await cache.GetStringAsync(key, cancellationToken);
            return json is null ? (null, false) : (JsonSerializer.Deserialize<List<EventDto>>(json, JsonOptions), true);
        }
        catch
        {
            return (null, false);
        }
    }

    public async Task SetAsync(int page, int pageSize, IReadOnlyList<EventDto> events, CancellationToken cancellationToken)
    {
        try
        {
            var key = await BuildKeyAsync(page, pageSize, cancellationToken);
            await cache.SetStringAsync(key, JsonSerializer.Serialize(events, JsonOptions),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2) }, cancellationToken);
        }
        catch { }
    }

    public Task InvalidateAsync(CancellationToken cancellationToken) =>
        cache.SetStringAsync(VersionKey, Guid.NewGuid().ToString("N"), cancellationToken);

    private async Task<string> BuildKeyAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var version = await cache.GetStringAsync(VersionKey, cancellationToken) ?? "initial";
        return $"events:{version}:{page}:{pageSize}";
    }
}
