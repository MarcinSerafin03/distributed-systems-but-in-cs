using RabbitMQ.Client;

namespace Lab6_RabbitMQ_cs.model;
public abstract class SystemParticipant
{
    protected IConnection? connection;
    protected IChannel? channel;
    protected string name;

    public SystemParticipant(string name)
    {
        this.name = name;
        var factory = new ConnectionFactory() { HostName = "localhost" };
        connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        channel = connection.CreateChannelAsync().GetAwaiter().GetResult();
        ConfigureInfrastructure().GetAwaiter().GetResult();
    }

    protected virtual async Task ConfigureInfrastructure()
    {
        await channel.ExchangeDeclareAsync(exchange: "orders", type: ExchangeType.Direct);
        
        await channel.ExchangeDeclareAsync(exchange: "confirmations", type: ExchangeType.Direct);
       
        await channel.ExchangeDeclareAsync(exchange: "admin", type: ExchangeType.Topic);
       
        await channel.ExchangeDeclareAsync(exchange: "monitoring", type: ExchangeType.Fanout);
    }

    public virtual async Task DisposeAsync()
    {
        if (channel != null)
            await channel.CloseAsync();
        if (connection != null)
            await connection.CloseAsync();
    }

    public virtual void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}