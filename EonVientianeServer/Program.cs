using System;
using System.Threading.Tasks;

namespace EonVientianeServer;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=================================");
        Console.WriteLine("  EonVientiane Game Server");
        Console.WriteLine("=================================\n");
        
        int port = 7777;
        
        // 从命令行参数读取端口
        if (args.Length > 0 && int.TryParse(args[0], out int customPort))
        {
            port = customPort;
        }
        
        var server = new GameServer(port);
        
        Console.WriteLine("Starting server...");
        await server.StartAsync();
        
        Console.WriteLine("\nServer is running. Available commands:");
        Console.WriteLine("  status  - Show server status");
        Console.WriteLine("  quit    - Stop server and exit");
        Console.WriteLine();
        
        // 命令行循环
        bool running = true;
        while (running)
        {
            Console.Write("> ");
            var command = Console.ReadLine()?.Trim().ToLower();
            
            switch (command)
            {
                case "status":
                    server.PrintStatus();
                    break;
                    
                case "quit":
                case "exit":
                    running = false;
                    break;
                    
                case "help":
                case "?":
                    Console.WriteLine("\nAvailable commands:");
                    Console.WriteLine("  status  - Show server status");
                    Console.WriteLine("  quit    - Stop server and exit");
                    Console.WriteLine();
                    break;
                    
                case "":
                    break;
                    
                default:
                    Console.WriteLine($"Unknown command: {command}");
                    Console.WriteLine("Type 'help' for available commands.");
                    break;
            }
        }
        
        Console.WriteLine("\nShutting down...");
        server.Stop();
        Console.WriteLine("Goodbye!");
    }
}
