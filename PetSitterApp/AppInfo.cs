using System.Reflection;
using Microsoft.JSInterop;

namespace PetSitterApp;

public static class AppInfo
{
    public static string Version { get; } = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion ?? "Unknown";

    public static event Action? OnUpdateAvailable;

    [JSInvokable("OnUpdateAvailable")]
    public static void TriggerUpdateAvailable()
    {
        OnUpdateAvailable?.Invoke();
    }
}
