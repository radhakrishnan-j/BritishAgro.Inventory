using System.Collections.ObjectModel;

namespace BritishAgro.Inventory.Services.Notifications;

public sealed class NotificationService
{
    private readonly List<NotificationMessage> _messages = new();
    public event Action? OnChange;

    public ReadOnlyCollection<NotificationMessage> Messages => _messages.AsReadOnly();

    public void Success(string message) => Show(message, NotificationType.Success);
    public void Info(string message) => Show(message, NotificationType.Info);
    public void Warning(string message) => Show(message, NotificationType.Warning);
    public void Error(string message) => Show(message, NotificationType.Error);

    public void Dismiss(Guid id)
    {
        var removed = _messages.RemoveAll(message => message.Id == id);
        if (removed > 0)
        {
            OnChange?.Invoke();
        }
    }

    private void Show(string message, NotificationType type)
    {
        var notification = new NotificationMessage
        {
            Message = message,
            Type = type
        };

        _messages.Insert(0, notification);
        OnChange?.Invoke();
        _ = AutoDismissAsync(notification.Id);
    }

    private async Task AutoDismissAsync(Guid id)
    {
        await Task.Delay(TimeSpan.FromSeconds(6));
        Dismiss(id);
    }
}
