namespace BritishAgro.Inventory.Services.Notifications;

public sealed class NotificationMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Message { get; init; } = string.Empty;
    public NotificationType Type { get; init; }
}

public enum NotificationType
{
    Success,
    Info,
    Warning,
    Error
}
