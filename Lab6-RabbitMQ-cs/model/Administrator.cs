using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Lab6_RabbitMQ_cs.model;
using System;
using System.Text;
using Newtonsoft.Json;
public class Administrator : SystemParticipant
{
    public Administrator() : base("Administrator")
    {
        InitializeAsync().GetAwaiter().GetResult();
        Console.WriteLine("[ADMINISTRATOR] Started - premium version");
    }

    private async Task InitializeAsync()
    {
        if (channel == null) return;
        
        var monitoringQueue = "monitoring_admin";
        await channel.QueueDeclareAsync(queue: monitoringQueue, durable: true, exclusive: false, autoDelete: false);
        await channel.QueueBindAsync(queue: monitoringQueue, exchange: "monitoring", routingKey: "");
       
        await ListenToAllMessages();
    }

    public async Task SendMessageToTeamsAsync(string content)
    {
        if (channel == null) return;
        
        var message = Message.CreateAdminMessage(content, "TEAMS");

        var messageJson = JsonConvert.SerializeObject(message);
        var body = Encoding.UTF8.GetBytes(messageJson);
        var properties = new BasicProperties();

        await channel.BasicPublishAsync(exchange: "admin", routingKey: "teams", mandatory: false, basicProperties: properties, body: body);
        Console.WriteLine($"[ADMINISTRATOR] Sent message to all Teams: {content}");
    }

    public void SendMessageToTeams(string content)
    {
        SendMessageToTeamsAsync(content).GetAwaiter().GetResult();
    }

    public async Task SendMessageToSuppliersAsync(string content)
    {
        if (channel == null) return;
        
        var message = Message.CreateAdminMessage(content, "SUPPLIERS");

        var messageJson = JsonConvert.SerializeObject(message);
        var body = Encoding.UTF8.GetBytes(messageJson);
        var properties = new BasicProperties();

        await channel.BasicPublishAsync(exchange: "admin", routingKey: "suppliers", mandatory: false, basicProperties: properties, body: body);
        Console.WriteLine($"[ADMINISTRATOR] Sent message to all Suppliers: {content}");
    }

    public void SendMessageToSuppliers(string content)
    {
        SendMessageToSuppliersAsync(content).GetAwaiter().GetResult();
    }

    public async Task SendMessageToAllAsync(string content)
    {
        if (channel == null) return;
        
        var message = Message.CreateAdminMessage(content, "ALL");

        var messageJson = JsonConvert.SerializeObject(message);
        var body = Encoding.UTF8.GetBytes(messageJson);
        var properties = new BasicProperties();

        await channel.BasicPublishAsync(exchange: "admin", routingKey: "all", mandatory: false, basicProperties: properties, body: body);
        Console.WriteLine($"[ADMINISTRATOR] Sent message to all participants: {content}");
    }

    public void SendMessageToAll(string content)
    {
        SendMessageToAllAsync(content).GetAwaiter().GetResult();
    }

    private async Task ListenToAllMessages()
    {
        if (channel == null) return;
        
        var monitoringQueue = "monitoring_admin";
        var consumer = new AsyncEventingBasicConsumer(channel);
       
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
           
            try
            {
                var msg = JsonConvert.DeserializeObject<Message>(message);
               
                if (msg != null)
                {
                    switch (msg.Type)
                    {
                        case MessageType.Order:
                            if (msg.OrderNumber == 0)
                            {
                                Console.WriteLine($"[ADMINISTRATOR] MONITORING - Order from {msg.TeamName} for {msg.EquipmentType}");
                            }
                            break;
                        case MessageType.Confirmation:
                            Console.WriteLine($"[ADMINISTRATOR] MONITORING - Confirmation #{msg.OrderNumber} " +
                                            $"from {msg.SupplierName} to {msg.TeamName}");
                            break;
                        default:
                            Console.WriteLine($"[ADMINISTRATOR] MONITORING - Unknown message type: {message}");
                            break;
                    }
                }
            }
            catch
            {
                Console.WriteLine($"[ADMINISTRATOR] MONITORING - Failed to parse message: {message}");
            }
            
            await Task.CompletedTask;
        };
       
        await channel.BasicConsumeAsync(queue: monitoringQueue, autoAck: true, consumerTag: "", noLocal: false, exclusive: false, arguments: null, consumer: consumer);
    }
}