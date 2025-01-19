namespace Client;

internal abstract class Program
{
    private static Task Main(string[] args)
    {
        var client = new TcpNotificationClient("127.0.0.1", 5000); // тут нужен адрес в локалке где запущен сервер

        Console.WriteLine("Press 'q' to quit.");
        _ = Task.Run(() => client.StartAsync());

        while (true)
        {
            var input = Console.ReadKey(true).Key;
            if (input != ConsoleKey.Q) continue;
            client.Stop();
            break;
        }

        return Task.CompletedTask;
    }
}
