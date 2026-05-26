using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace DaylogDockExtension;

public partial class DaylogEditorApp : Application, IDisposable
{
    private DaylogEditorWindow? _window;
    private EventWaitHandle? _showEvent;
    private CancellationTokenSource? _showListenerCancellation;

    public DaylogEditorApp()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new DaylogEditorWindow();
        _window.Closed += (_, _) => Dispose();
        _window.Activate();
        if (!DaylogLaunchOptions.Startup)
        {
            _window.BringToFront();
        }

        StartShowListener();
    }

    private void StartShowListener()
    {
        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, EditorSingleInstance.ShowEventName);
        _showListenerCancellation = new CancellationTokenSource();
        var token = _showListenerCancellation.Token;
        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _ = Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                if (!_showEvent.WaitOne(250))
                {
                    continue;
                }

                dispatcherQueue.TryEnqueue(() => _window?.ToggleFromDockActivation());
            }
        }, token);
    }

    public void Dispose()
    {
        _showListenerCancellation?.Cancel();
        _showListenerCancellation?.Dispose();
        _showEvent?.Dispose();
        GC.SuppressFinalize(this);
    }
}
