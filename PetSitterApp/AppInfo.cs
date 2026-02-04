using Microsoft.JSInterop;

namespace PetSitterApp;

public static class AppInfo
{
    public const string Version = "2024-05-23";
    public static event Action? OnUpdateAvailable;

    [JSInvokable("OnUpdateAvailable")]
    public static void TriggerUpdateAvailable()
    {
        OnUpdateAvailable?.Invoke();
    }
    public const string Version = "2024-05-22-01";
}
