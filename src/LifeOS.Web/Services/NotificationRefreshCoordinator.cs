namespace LifeOS.Web.Services;

public sealed class NotificationRefreshCoordinator
{
    public event EventHandler? RefreshRequested;

    public IDisposable Subscribe(EventHandler handler)
    {
        RefreshRequested += handler;
        return new Subscription(this, handler);
    }

    public void RequestRefresh()
    {
        RefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    private sealed class Subscription : IDisposable
    {
        private readonly NotificationRefreshCoordinator _owner;
        private readonly EventHandler _handler;
        private bool _disposed;

        public Subscription(NotificationRefreshCoordinator owner, EventHandler handler)
        {
            _owner = owner;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _owner.RefreshRequested -= _handler;
            _disposed = true;
        }
    }
}
