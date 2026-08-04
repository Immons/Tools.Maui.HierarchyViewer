namespace Immons.Tools.Maui.Inspector.Web.Dispatch;

/// <summary>Marshals endpoint work onto the MAUI main thread.</summary>
internal interface IMainThreadDispatcher
{
    Task<T> RunAsync<T>(Func<T> func);

    Task<T> RunTaskAsync<T>(Func<Task<T>> func);
}
