namespace Lab6_RabbitMQ_cs.model;

public class Message
{
    public MessageType Type { get; set; }
    public string TeamName { get; set; }
    public string EquipmentType { get; set; }
    public int OrderNumber { get; set; }
    public string SupplierName { get; set; }
    public string Content { get; set; }
    public string RecipientType { get; set; } // "TEAMS", "SUPPLIERS", "ALL"

    public static Message CreateOrder(string teamName, string equipmentType)
    {
        return new Message
        {
            Type = MessageType.Order,
            TeamName = teamName,
            EquipmentType = equipmentType,
            OrderNumber = 0
        };
    }

    public static Message CreateConfirmation(string teamName, string supplierName, int orderNumber, string equipmentType)
    {
        return new Message
        {
            Type = MessageType.Confirmation,
            TeamName = teamName,
            SupplierName = supplierName,
            OrderNumber = orderNumber,
            EquipmentType = equipmentType
        };
    }

    public static Message CreateAdminMessage(string content, string recipientType)
    {
        return new Message
        {
            Type = MessageType.AdminMessage,
            Content = content,
            RecipientType = recipientType
        };
    }
}