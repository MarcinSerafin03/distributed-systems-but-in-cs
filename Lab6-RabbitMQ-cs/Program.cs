using Lab6_RabbitMQ_cs.model;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== MOUNTAIN EXPEDITION SYSTEM - RabbitMQ ===\n");
       
        try
        {
            Console.WriteLine("--- STARTING SYSTEM ---");
           
            var team1 = new Team("Team 1");
            var team2 = new Team("Team 2");
           
            var supplier1 = new Supplier("Supplier 1", new List<string> { "oxygen", "boots" });
            var supplier2 = new Supplier("Supplier 2", new List<string> { "oxygen", "backpack" });
           
            var administrator = new Administrator();
           
            await Task.Delay(2000);
           
            Console.WriteLine("\n--- ORDER SERIES FROM TEAM ALPINEEXPLORERS ---");
           
            string[] orders = { "oxygen", "oxygen", "boots", "boots", "backpack", "backpack" };
           
            foreach (var order in orders)
            {
                await team1.PlaceOrderAsync(order);
                await Task.Delay(500);
            }
           
            await Task.Delay(2000);
           
            Console.WriteLine("\n--- ADMINISTRATIVE MESSAGES ---");
           
            await administrator.SendMessageToTeamsAsync("Message to all Teams");
            await Task.Delay(500);
           
            await administrator.SendMessageToSuppliersAsync("Message to all Suppliers");
            await Task.Delay(500);
           
            await administrator.SendMessageToAllAsync("Message to EVERYONE");
            await Task.Delay(1000);
           
            Console.WriteLine("\n--- END OF DEMONSTRATION ---");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
           
            await team1.DisposeAsync();
            await team2.DisposeAsync();
            await supplier1.DisposeAsync();
            await supplier2.DisposeAsync();
            await administrator.DisposeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine("Make sure RabbitMQ is running on localhost.");
        }
    }
}