using System;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace DaylogDockExtension;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        DaylogLaunchOptions.Apply(args);

        if (!EditorSingleInstance.TryClaim(out var singleInstanceMutex))
        {
            using (singleInstanceMutex)
            {
                if (!EditorSingleInstance.SignalExistingEditor())
                {
                    Environment.ExitCode = 1;
                }

                return;
            }
        }

        using (singleInstanceMutex)
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();

            Application.Start(_ =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);

                var app = new DaylogEditorApp();
                GC.KeepAlive(app);
            });
        }
    }
}
