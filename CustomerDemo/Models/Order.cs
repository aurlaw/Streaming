using CustomerDemo.Attributes;

namespace CustomerDemo.Models;

[GenerateSerializer("O")]
public class Order
{
    [ReportItem(1, 10, PaddingChar = '0', PaddingDirection = PaddingDirection.Right)]
    public string OrderNumber { get; set; } = string.Empty;
    
    [ReportItem(2, 5, PaddingChar = '0', PaddingDirection = PaddingDirection.Left)]
    public int TotalItems { get; set; }
    
    [ReportItem(3, 10, PaddingChar = '0', PaddingDirection = PaddingDirection.Left)]
    public decimal TotalPrice { get; set; }    
    public List<OrderLine> OrderLines { get; set; } = new();    
}