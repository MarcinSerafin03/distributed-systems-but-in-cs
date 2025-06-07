using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Lab6_RabbitMQ_cs.model;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
public class Supplier : SystemParticipant
{
    private List<string> supportedEquipmentTypes;
    private int orderNumber = 1;

    public Supplier(string name, List<string> equipmentTypes) : base(name)
    {
        this.supportedEquipmentTypes = equipmentTypes;
        InitializeAsync().GetAwaiter().GetResult();
        Console.WriteLine($"[SUPPLIER {name}] Started, handles: {string.Join(", ", equipmentTypes)}");
    }

    private async Task InitializeAsync()
    {
        if (channel == null) return;
        
        foreach (var type in supportedEquipmentTypes)
        {
            var queue = $"orders_{type}_{name}";
            await channel.QueueDeclareAsync(queue: queue, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(queue: queue, exchange: "orders", routingKey: type);
        }
       
        var adminQueue = $"admin_supplier_{name}";
        await channel.QueueDeclareAsync(queue: adminQueue, durable: true, exclusive: false, autoDelete: false);
        await channel.QueueBindAsync(queue: adminQueue, exchange: "admin", routingKey: "suppliers");
        await channel.QueueBindAsync(queue: adminQueue, exchange: "admin", routingKey: "all");
       
        await ListenForOrders();
        await ListenForAdminMessages();
    }

    private async Task ListenForOrders()
    {
        if (channel == null) return;
        
        foreach (var type in supportedEquipmentTypes)
        {
            var queue = $"orders_{type}_{name}";
            var consumer = new AsyncEventingBasicConsumer(channel);
           
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var order = JsonConvert.DeserializeObject<Message>(message);
               
                if (order != null)
                {
                    order.OrderNumber = orderNumber++;
                    order.SupplierName = name;
                   
                    Console.WriteLine($"[SUPPLIER {name}] Received order #{order.OrderNumber} " +
                                    $"for {order.EquipmentType} from {order.TeamName}");
                   
                    await ProcessOrderAsync(order);
                }
            };
           
            await channel.BasicConsumeAsync(queue: queue, autoAck: true, consumerTag: "", noLocal: false, exclusive: false, arguments: null, consumer: consumer);
        }
    }

    private async Task ProcessOrderAsync(Message order)
    {
        if (channel == null) return;
        
        Console.WriteLine($"[SUPPLIER {name}] Processed order #{order.OrderNumber}");
       
        var confirmation = Message.CreateConfirmation(order.TeamName ?? "", name, order.OrderNumber, order.EquipmentType ?? "");

        var message = JsonConvert.SerializeObject(confirmation);
        var body = Encoding.UTF8.GetBytes(message);
        var properties = new BasicProperties();

        await channel.BasicPublishAsync(exchange: "confirmations", routingKey: order.TeamName ?? "", mandatory: false, basicProperties: properties, body: body);
       
        await channel.BasicPublishAsync(exchange: "monitoring", routingKey: "", mandatory: false, basicProperties: properties, body: body);
       
        Console.WriteLine($"[SUPPLIER {name}] Sent confirmation for order #{order.OrderNumber} " +
                        $"to {order.TeamName}");
    }

    private async Task ListenForAdminMessages()
    {
        if (channel == null) return;
        
        var adminQueue = $"admin_supplier_{name}";
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var adminMsg = JsonConvert.DeserializeObject<Message>(message);
           
            Console.WriteLine($"[SUPPLIER {name}] Admin message: {adminMsg?.Content}");
            
            await Task.CompletedTask;
        };
       
        await channel.BasicConsumeAsync(queue: adminQueue, autoAck: true, consumerTag: "", noLocal: false, exclusive: false, arguments: null, consumer: consumer);
    }
}
