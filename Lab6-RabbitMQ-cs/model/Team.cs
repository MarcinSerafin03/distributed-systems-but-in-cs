using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Lab6_RabbitMQ_cs.model;
using System;
using System.Text;
using Newtonsoft.Json;
public class Team : SystemParticipant
{
    private string confirmationQueue;

    public Team(string name) : base(name)
    {
        confirmationQueue = $"confirmations_{name}";
        InitializeAsync().GetAwaiter().GetResult();
        Console.WriteLine($"[TEAM {name}] Started");
    }

    private async Task InitializeAsync()
    {
        if (channel == null) return;
        
        await channel.QueueDeclareAsync(queue: confirmationQueue, durable: true, exclusive: false, autoDelete: false);
        await channel.QueueBindAsync(queue: confirmationQueue, exchange: "confirmations", routingKey: name);
       
        var adminQueue = $"admin_team_{name}";
        await channel.QueueDeclareAsync(queue: adminQueue, durable: true, exclusive: false, autoDelete: false);
        await channel.QueueBindAsync(queue: adminQueue, exchange: "admin", routingKey: "teams");
        await channel.QueueBindAsync(queue: adminQueue, exchange: "admin", routingKey: "all");
       
        await ListenForConfirmations();
        await ListenForAdminMessages();
    }

    public async Task PlaceOrderAsync(string equipmentType)
    {
        if (channel == null) return;
        
        var order = Message.CreateOrder(name, equipmentType);

        var message = JsonConvert.SerializeObject(order);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = new BasicProperties();
        await channel.BasicPublishAsync(exchange: "orders", routingKey: equipmentType, mandatory: false, basicProperties: properties, body: body);
       
        await channel.BasicPublishAsync(exchange: "monitoring", routingKey: "", mandatory: false, basicProperties: properties, body: body);
       
        Console.WriteLine($"[TEAM {name}] Sent order for: {equipmentType}");
    }

    public void PlaceOrder(string equipmentType)
    {
        PlaceOrderAsync(equipmentType).GetAwaiter().GetResult();
    }

    private async Task ListenForConfirmations()
    {
        if (channel == null) return;
        
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var confirmation = JsonConvert.DeserializeObject<Message>(message);
           
            Console.WriteLine($"[TEAM {name}] Received confirmation for order #{confirmation?.OrderNumber} " +
                            $"for {confirmation?.EquipmentType} from {confirmation?.SupplierName}");
            
            await Task.CompletedTask;
        };
       
        await channel.BasicConsumeAsync(queue: confirmationQueue, autoAck: true, consumerTag: "", noLocal: false, exclusive: false, arguments: null, consumer: consumer);
    }

    private async Task ListenForAdminMessages()
    {
        if (channel == null) return;
        
        var adminQueue = $"admin_team_{name}";
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var adminMsg = JsonConvert.DeserializeObject<Message>(message);
           
            Console.WriteLine($"[TEAM {name}] Admin message: {adminMsg?.Content}");
            
            await Task.CompletedTask;
        };
       
        await channel.BasicConsumeAsync(queue: adminQueue, autoAck: true, consumerTag: "", noLocal: false, exclusive: false, arguments: null, consumer: consumer);
    }
}