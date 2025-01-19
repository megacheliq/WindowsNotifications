using Serilog;

namespace Server;

internal class Program
{
    private static async Task Main(string[] args)
    {
        ConfigureLogging();

        try
        {
            Log.Information("Starting application...");

            var server = new TcpNotificationServer(5000);
            server.Start();

            Log.Information("Server is running. Press 'n' to send a notification, or 'q' to quit.");

            await HandleUserInputAsync(server);

            Log.Information("Application is shutting down...");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static void ConfigureLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/server.log", rollingInterval: RollingInterval.Day)
            .Enrich.FromLogContext()
            .CreateLogger();
    }

    private static async Task HandleUserInputAsync(TcpNotificationServer server)
    {
        while (true)
        {
            Console.WriteLine("Enter 'n' to send a notification or 'q' to quit:");
            var input = Console.ReadKey(true).Key;

            switch (input)
            {
                case ConsoleKey.N:
                    Console.Write("Enter notification message: ");
                    var message = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        server.SendNotification(message);
                        Log.Information("Notification sent: {Message}", message);
                    }
                    else
                    {
                        Log.Warning("Empty message, notification was not sent.");
                    }
                    break;

                case ConsoleKey.Q:
                    server.Stop();
                    return;

                default:
                    Console.WriteLine("Invalid input. Press 'n' to send a notification or 'q' to quit.");
                    break;
            }

            await Task.Delay(100);
        }
    }
}
