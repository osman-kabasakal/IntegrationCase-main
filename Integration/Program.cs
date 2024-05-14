using Integration.Common;
using Integration.Service;

namespace Integration;

public abstract class Program
{
    static ItemIntegrationService _service = new ItemIntegrationService();
    public static void Main(string[] args)
    {
        for(int i = 0; i < 5; i++)
        {
            var newThread = new Thread(new ThreadStart(ThreadProc))
            {
                Name = $"Thread{i + 1}"
            };
            newThread.Start();
        }
        
        Thread.Sleep(10000);
        
        Console.WriteLine("Everything recorded:");
        
        var allItems=_service.GetAllItems();
            allItems.ForEach(Console.WriteLine);
        
        Console.ReadLine();
        
    }

    static void ThreadProc()
    {
        ThreadPool.QueueUserWorkItem(_ => _service.SaveItem("a").Wait());
        ThreadPool.QueueUserWorkItem(_ => _service.SaveItem("b").Wait());
        ThreadPool.QueueUserWorkItem(_ => _service.SaveItem("c").Wait());
    }
}