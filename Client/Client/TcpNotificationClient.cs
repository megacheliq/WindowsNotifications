using System.Net.Sockets;
using System.Text;
using Windows.UI.Notifications;

public class TcpNotificationClient
{
    private readonly string _serverIp;
    private readonly int _serverPort;
    private TcpClient? _client;

    public TcpNotificationClient(string serverIp, int serverPort)
    {
        _serverIp = serverIp;
        _serverPort = serverPort;
    }

    public async Task StartAsync()
    {
        while (true)
        {
            try
            {
                Console.WriteLine("Connecting to server...");

                _client = new TcpClient();
                await _client.ConnectAsync(_serverIp, _serverPort);

                Console.WriteLine($"Connected to server {_serverIp}:{_serverPort}");
                await ListenForNotificationsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to server: {ex.Message}");
            }

            Console.WriteLine("Reconnecting in 10 seconds...");
            await Task.Delay(TimeSpan.FromSeconds(10));
        }
    }
    
    private async Task ListenForNotificationsAsync()
    {
        if (_client is null)
            return;
        
        var buffer = new byte[1024];
        var stream = _client.GetStream();

        while (_client.Connected)
        {
            try
            {
                var bytesRead = await stream.ReadAsync(buffer);
                if (bytesRead == 0)
                {
                    Console.WriteLine("Disconnected from server.");
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                ShowNotification("Уведомление от сервера", message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving data: {ex.Message}");
                break;
            }
        }

        _client.Close();
    }

    private void ShowNotification(string title, string message)
    {
        var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
        var textNodes = toastXml.GetElementsByTagName("text");
        
        textNodes[0].AppendChild(toastXml.CreateTextNode(title));  // Заголовок
        textNodes[1].AppendChild(toastXml.CreateTextNode(message)); // Текст уведомления

        var toast = new ToastNotification(toastXml);
        
        toast.Activated += (s, e) =>
        {
            Console.WriteLine($"Уведомление '{message}' прочитано.");
            SendReadConfirmation(message);
        };
        
        ToastNotificationManager.CreateToastNotifier("TcpNotificationClient").Show(toast);
    }
    
    private void SendReadConfirmation(string message)
    {
        if (_client?.Connected != true)
        {
            Console.WriteLine("Cannot send confirmation, client is not connected.");
            return;
        }
        
        try
        {
            var stream = _client.GetStream();
            var confirmationMessage = Encoding.UTF8.GetBytes($"Read:{message}");
            stream.Write(confirmationMessage, 0, confirmationMessage.Length);
            Console.WriteLine("Confirmation sent to server.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending confirmation: {ex.Message}");
        }
    }

    public void Stop()
    {
        try
        {
            _client?.Close();
            Console.WriteLine("Client stopped.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while stopping client: {ex.Message}");
        }
    }
}