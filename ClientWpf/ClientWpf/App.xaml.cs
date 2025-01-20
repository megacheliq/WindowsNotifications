using System.Windows;

namespace ClientWpf
{
    public partial class App : Application
    {
        private TcpNotificationClient? _tcpNotificationClient;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _tcpNotificationClient = new TcpNotificationClient("192.168.0.103", 5000);
            Task.Run(() => _tcpNotificationClient.StartAsync());
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _tcpNotificationClient?.Stop();
            base.OnExit(e);
        }
    }
}