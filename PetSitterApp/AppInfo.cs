using Microsoft.JSInterop;

namespace PetSitterApp;

public static class AppInfo
{
    public const string Version = "2026-02-04 12:00";
    public static event Action? OnUpdateAvailable;

    [JSInvokable("OnUpdateAvailable")]
    public static void TriggerUpdateAvailable()
    {
        OnUpdateAvailable?.Invoke();
    }
}
