using Serilog;

namespace Server;

class Program
{
    static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console() // Логирование в консоль
            .WriteTo.File("logs/server.log", rollingInterval: RollingInterval.Day) // Логирование в файл с ежедневной ротацией
            .CreateLogger();
        
        try
        {
            Log.Information("Starting application...");

            var server = new TcpNotificationServer(5000);
            server.Start();

            Console.WriteLine("Press 'n' to send a notification, or 'q' to quit.");

            while (true)
            {
                var input = Console.ReadKey(true).Key;
                if (input == ConsoleKey.N)
                {
                    Console.Write("Enter notification message: ");
                    var message = Console.ReadLine();
                    if (message != null) server.SendNotification(message);
                }
                else if (input == ConsoleKey.Q)
                {
                    server.Stop();
                    break;
                }
            }
            
            await Task.CompletedTask;
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
}