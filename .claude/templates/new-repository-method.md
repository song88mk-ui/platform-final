# Template: New Repository Method

## Shape
```csharp
// IXxxRepository.cs
Task<BaseResponseDTO<TResult>> GetThing(TQuery query, CancellationToken ct = default);

// XxxRepository.cs
public async Task<BaseResponseDTO<TResult>> GetThing(TQuery query, CancellationToken ct = default)
{
    try
    {
        // EF read: AsNoTracking + manual .Select projection
        var result = await _context.Things.AsNoTracking()
            .Where(...)
            .Select(t => new TResult { ... })
            .ToListAsync(ct);
        return new BaseResponseDTO<TResult> { StatusCode = StatusCode.Success, Result = ... };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "GetThing failed {@query}", query);
        return new BaseResponseDTO<TResult> { StatusCode = StatusCode.Error, Message = "..." }; // no StackTrace
    }
}
```

## Rules
- **Reads:** `AsNoTracking` + projection. **Multi-write:** `TransactionScope` (ReadCommitted).
- **Stored procs:** parameterized — `FromSqlRaw("Proc {0}", arg)` / `SqlParameter`. **Never interpolate** (AP-2).
- **Money:** Wallet stored procs only (`decimal(26,12)`). **Mongo:** read-only `.Find().Project<T>()`.
- **Cache:** wrap reads with `IMemCache.GetCache(key, loader, hours)` using a prefix that `CacheSync`
  can evict. **Register:** `AddScoped<IXxxRepository, XxxRepository>` (or typed `HttpClient`).
