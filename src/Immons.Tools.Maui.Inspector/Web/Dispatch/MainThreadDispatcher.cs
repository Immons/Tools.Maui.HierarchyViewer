namespace Immons.Tools.Maui.Inspector.Web.Dispatch;

/// <summary>Dispatches onto the active window's dispatcher (fallback: application dispatcher).</summary>
internal sealed class MainThreadDispatcher(IActiveInspectorProvider inspectors) : IMainThreadDispatcher
{
    public Task<T> RunAsync<T>(Func<T> func)
    {
        if (Resolve() is not { } dispatcher)
            return Task.FromResult(func());

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        dispatcher.Dispatch(() =>
        {
            try
            {
                tcs.SetResult(func());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }

    public Task<T> RunTaskAsync<T>(Func<Task<T>> func)
    {
        if (Resolve() is not { } dispatcher)
            return func();

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        dispatcher.Dispatch(async void () =>
        {
            try
            {
                tcs.SetResult(await func());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }

    IDispatcher? Resolve() => inspectors.Current?.Dispatcher ?? Application.Current?.Dispatcher;
}
