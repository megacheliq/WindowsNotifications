using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Serilog;

namespace Server;

public class TcpNotificationServer
{
    private readonly TcpListener _listener;
    private readonly ConcurrentBag<TcpClient> _connectedClients = new();
    private bool _isRunning;

    public TcpNotificationServer(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
    }
    
    public void Start()
    {
        Log.Information("Starting server...");
        _listener.Start();
        _isRunning = true;

        Log.Information("Server started on {LocalEndpoint}.", _listener.LocalEndpoint);
        Log.Information("Waiting for clients to connect...");

        Task.Run(async () =>
        {
            while (_isRunning)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    Log.Information("Client connected: {RemoteEndPoint}", client.Client.RemoteEndPoint);

                    _connectedClients.Add(client);
                    _ = HandleClientAsync(client);
                }
                catch (Exception ex) when (_isRunning)
                {
                    Log.Error(ex, "Error accepting new client.");
                }
            }
        });
    }
    
    private async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            client.ReceiveTimeout = 10000; // 10 секунд
            client.SendTimeout = 10000;

            var buffer = new byte[1024];
            var stream = client.GetStream();

            while (_isRunning && client.Connected)
            {
                var bytesRead = await stream.ReadAsync(buffer);
                
                if (bytesRead == 0)
                {
                    break; // Клиент отключился
                }

                var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                ProcessMessage(message, client);
            }
        }
        catch (IOException ioEx)
        {
            Log.Warning("Client {RemoteEndPoint} disconnected unexpectedly: {Message}", 
                client.Client.RemoteEndPoint, ioEx.Message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error handling client {RemoteEndPoint}", client.Client.RemoteEndPoint);
        }
        finally
        {
            _connectedClients.TryTake(out var _); // Удаляем клиента из списка
            client.Close();
            Log.Information("Client disconnected: {RemoteEndPoint}", client.Client.RemoteEndPoint);
        }
    }

    public void SendNotification(string message)
    {
        var data = Encoding.UTF8.GetBytes(message);
        foreach (var client in _connectedClients)
        {
            if (!client.Connected) continue;

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
        _isRunning = false;

        foreach (var client in _connectedClients)
        {
            client.Close();
        }

        _listener.Stop();
        Log.Information("Server stopped.");
    }

    private static void ProcessMessage(string message, TcpClient client)
    {
        if (message.StartsWith("Read:"))
        {
            var notificationContent = message[5..];
            Log.Information("Notification read by client {ClientInfo}: {NotificationContent}",
                client.Client.RemoteEndPoint?.ToString(), notificationContent);
        }
        else
        {
            Log.Warning("Unknown message format from {RemoteEndPoint}: {Message}", client.Client.RemoteEndPoint,
                message);
        }
    }
}