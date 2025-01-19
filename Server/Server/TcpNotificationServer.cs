using System.Net;
using System.Net.Sockets;
using System.Text;
using Serilog;

namespace Server;

public class TcpNotificationServer
{
    private readonly TcpListener _listener;
    private readonly List<TcpClient> _connectedClients = [];

    public TcpNotificationServer(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port); // Принимаем подключения с любого IP
    }
    
    public void Start()
    {
        Log.Information("Starting server...");
        
        _listener.Start();
        
        Log.Information("Server started on {LocalEndpoint}.", _listener.LocalEndpoint);
        Log.Information("Waiting for clients to connect...");

        Task.Run(async () =>
        {
            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync();
                Log.Information("Client connected: {RemoteEndPoint}", client.Client.RemoteEndPoint);

                _connectedClients.Add(client);
                await HandleClientAsync(client); // Обработка клиента
            }
        });
    }
    
    private async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            var buffer = new byte[1024];
            var stream = client.GetStream();

            while (client.Connected)
            {
                // Получаем данные от клиента
                var bytesRead = await stream.ReadAsync(buffer);
                
                if (bytesRead == 0)
                {
                    break; // Клиент отключился
                }
                
                // Отправляем ответ (если нужно)
                var response = Encoding.UTF8.GetBytes("Message received");
                await stream.WriteAsync(response);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error handling client {RemoteEndPoint}", client.Client.RemoteEndPoint);
        }
        finally
        {
            Log.Information("Client disconnected: {RemoteEndPoint}", client.Client.RemoteEndPoint);
            _connectedClients.Remove(client);
            client.Close();
        }
    }

    public void SendNotification(string message)
    {
        var data = Encoding.UTF8.GetBytes(message);
        foreach (var client in _connectedClients.Where(client => client.Connected))
        {
            try
            {
                var stream = client.GetStream();
                stream.Write(data, 0, data.Length);
                Log.Information("Notification sent to {RemoteEndPoint}", client.Client.RemoteEndPoint);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to send to {RemoteEndPoint}", client.Client.RemoteEndPoint);
            }
        }
    }

    public void Stop()
    {
        foreach (var client in _connectedClients)
        {
            client.Close();
        }
        _listener.Stop();
        Log.Information("Server stopped.");
    }
}