using Microsoft.JSInterop;

namespace BritishAgro.Inventory.Services.Time;

public sealed class BrowserTimeService(IJSRuntime jsRuntime)
{
    private int? _browserOffsetMinutes;
    private bool _isInitializing;

    public event Action? OnChange;

    public bool IsInitialized => _browserOffsetMinutes.HasValue;

    public async Task EnsureInitializedAsync()
    {
        if (IsInitialized || _isInitializing)
        {
            return;
        }

        _isInitializing = true;
        try
        {
            _browserOffsetMinutes = await jsRuntime.InvokeAsync<int>("inventoryTime.getBrowserOffsetMinutes");
            OnChange?.Invoke();
        }
        finally
        {
            _isInitializing = false;
        }
    }

    public long ToUtcUnixMilliseconds(DateTime browserLocalDateTime)
    {
        var offset = GetBrowserOffset();
        var localOffset = TimeSpan.FromMinutes(-offset);
        var unspecifiedLocal = DateTime.SpecifyKind(browserLocalDateTime, DateTimeKind.Unspecified);
        return new DateTimeOffset(unspecifiedLocal, localOffset).ToUnixTimeMilliseconds();
    }

    public DateTime ToBrowserLocalDateTime(long utcUnixMilliseconds)
    {
        var offset = GetBrowserOffset();
        return DateTimeOffset
            .FromUnixTimeMilliseconds(utcUnixMilliseconds)
            .ToOffset(TimeSpan.FromMinutes(-offset))
            .DateTime;
    }

    public string Format(long? utcUnixMilliseconds, string format = "dd MMM yyyy hh:mm tt")
    {
        if (!utcUnixMilliseconds.HasValue)
        {
            return "-";
        }

        if (!IsInitialized)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(utcUnixMilliseconds.Value).UtcDateTime.ToString($"{format} 'UTC'");
        }

        return ToBrowserLocalDateTime(utcUnixMilliseconds.Value).ToString(format);
    }

    private int GetBrowserOffset()
    {
        if (!_browserOffsetMinutes.HasValue)
        {
            throw new InvalidOperationException("Browser time zone information has not been initialized yet.");
        }

        return _browserOffsetMinutes.Value;
    }
}
