using System.Media;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ClientWpf;

public partial class NotificationWindow : Window
{
    private readonly TcpNotificationClient _client;
    private readonly string _message;
    private DispatcherTimer _flashTimer;
    private bool _isSoundPlaying = true;
    private SolidColorBrush _red = new SolidColorBrush(Colors.Red);
    private SolidColorBrush _yellow = new SolidColorBrush(Colors.Yellow);

    public NotificationWindow(string title, string message, TcpNotificationClient client)
    {
        InitializeComponent();
        TitleBlock.Text = title;
        MessageBlock.Text = message;
        _client = client;
        _message = message;
        
        SetupFlashing();
        PlayAnnoyingSoundInLoop();
        
        Loaded += OnLoaded;
    }
    
    private void SetupFlashing()
    {
        _flashTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _flashTimer.Tick += FlashWindow;
        _flashTimer.Start();
    }
    
    private void FlashWindow(object? sender, EventArgs e)
    {
        GridBackground.Background = GridBackground.Background == _red ? _yellow : _red;
    }

    private async void PlayAnnoyingSoundInLoop()
    {
        while (_isSoundPlaying)
        {
            SystemSounds.Beep.Play();

            await Task.Delay(1000);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Left = SystemParameters.WorkArea.Left + 10;
        Top = SystemParameters.WorkArea.Bottom - Height - 10;
    }

    private void CloseNotification(object sender, RoutedEventArgs e)
    {
        _client.SendReadConfirmation(_message);
        _flashTimer.Stop();
        _isSoundPlaying = false;
        Close();
    }
}