namespace Orders.Models.Enums;

public enum OrderStatus
{
    Draft = 0,
    Confirmed = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}