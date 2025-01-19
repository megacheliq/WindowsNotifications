using System.Net.Sockets;
using System.Text;
using Windows.UI.Notifications;

public class TcpNotificationClient
{
    private readonly string _serverIp;
    private readonly int _serverPort;
    private TcpClient _client;

    public TcpNotificationClient(string serverIp, int serverPort)
    {
        _serverIp = serverIp;
        _serverPort = serverPort;
    }

    public async Task StartAsync()
    {
        try
        {
            Console.WriteLine("Connecting to server...");
            
            _client = new TcpClient();
            await _client.ConnectAsync(_serverIp, _serverPort);
            
            Console.WriteLine("Connected to server.");

            await ListenForNotificationsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to server: {ex.Message}");
        }
    }

    private async Task ListenForNotificationsAsync()
    {
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

    private static void ShowNotification(string title, string message)
    {
        var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
        var textNodes = toastXml.GetElementsByTagName("text");
        
        textNodes[0].AppendChild(toastXml.CreateTextNode(title));  // Заголовок
        textNodes[1].AppendChild(toastXml.CreateTextNode(message)); // Текст уведомления

        var toast = new ToastNotification(toastXml);
        ToastNotificationManager.CreateToastNotifier("TcpNotificationClient").Show(toast);
    }

    public void Stop()
    {
        _client?.Close();
        Console.WriteLine("Client stopped.");
    }
}
