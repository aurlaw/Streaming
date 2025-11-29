using CustomerDemo.Attributes;

namespace CustomerDemo.Models;

[GenerateSerializer("C")]
public class Customer
{
    [ReportItem(1, 50, PaddingDirection = PaddingDirection.Right)]
    public string FirstName { get; set; } = string.Empty;
    
    [ReportItem(2, 50, PaddingDirection = PaddingDirection.Right)]
    public string LastName { get; set; } = string.Empty;
    
    [ReportItem(3, 200, PaddingDirection = PaddingDirection.Right)]
    public string ShippingAddress { get; set; } = string.Empty;
    
    [ReportItem(4, 50, PaddingDirection = PaddingDirection.Right)]
    public string ShippingCity { get; set; } = string.Empty;
    
    [ReportItem(5, 2, ' ', PaddingDirection.Right)]
    public string ShippingState { get; set; } = string.Empty;
    
    [ReportItem(6, 5, ' ', PaddingDirection.Right)]
    public string ShippingZip { get; set; } = string.Empty;    
    [ReportItem(6, 10, PaddingDirection = PaddingDirection.Right)]
    public string ShippingPhone { get; set; } = string.Empty;    
    public List<Order> Orders { get; set; } = new();    
}