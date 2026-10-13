using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Velopack;

namespace CoopWarrior;

public static class Program
{
    [STAThread]
    static void Main()
    {
        // Setup.exe and the updater start the app with special arguments; Velopack handles those and exits.
        VelopackApp.Build().Run();

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
    }
}
