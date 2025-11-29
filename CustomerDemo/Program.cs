// See https://aka.ms/new-console-template for more information


using CustomerDemo.Models;
using CustomerDemo.Serialization;

Console.WriteLine("=== Fixed Position File Generator Comparison ===\n");
Console.WriteLine("1. Run Demo");
Console.WriteLine("2. Run Performance Comparison");
Console.WriteLine("3. Run BenchmarkDotNet (detailed benchmarks)");
Console.WriteLine("4. Exit");
Console.Write("\nSelect an option: ");

var choice = Console.ReadLine();

switch (choice)
{
    case "1":
        Console.WriteLine("Run Demo");
        break;
    case "2":
        Console.WriteLine("Run Performance Comparison");
        break;
    case "3":
        Console.WriteLine("Run BenchmarkDotNet (detailed benchmarks)");
        break;
    case "4":
        return;
    default:
        Console.WriteLine("Invalid option");
        break;
}

var data = CreateSampleData();
Console.WriteLine(data.Count);

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();


// Program.cs - Update CreateSampleData method
static List<Customer> CreateSampleData()
{
    return new List<Customer>
    {
        new Customer
        {
            FirstName = "John",
            LastName = "Smith",
            ShippingAddress = "123 Main Street",
            ShippingCity = "Pittsburgh",
            ShippingState = "PA",
            ShippingZip = "15216",
            ShippingPhone = "4125551234",
            Orders = new List<Order>
            {
                new Order
                {
                    OrderNumber = "ORD001",
                    TotalItems = 3,
                    TotalPrice = 125.50m,
                    OrderLines = new List<OrderLine>
                    {
                        new OrderLine
                        {
                            ProductName = "Widget A",
                            Quantity = 2,
                            Price = 25.00m
                        },
                        new OrderLine
                        {
                            ProductName = "Widget B",
                            Quantity = 1,
                            Price = 75.50m
                        }
                    }
                },
                new Order
                {
                    OrderNumber = "ORD002",
                    TotalItems = 1,
                    TotalPrice = 200.00m,
                    OrderLines = new List<OrderLine>
                    {
                        new OrderLine
                        {
                            ProductName = "Premium Gadget",
                            Quantity = 1,
                            Price = 200.00m
                        }
                    }
                }
            }
        },
        new Customer
        {
            FirstName = "Jane",
            LastName = "Doe",
            ShippingAddress = "456 Oak Avenue",
            ShippingCity = "Philadelphia",
            ShippingState = "PA",
            ShippingZip = "19103",
            ShippingPhone = "2155559876",
            Orders = new List<Order>
            {
                new Order
                {
                    OrderNumber = "ORD003",
                    TotalItems = 5,
                    TotalPrice = 89.99m,
                    OrderLines = new List<OrderLine>
                    {
                        new OrderLine
                        {
                            ProductName = "Small Widget",
                            Quantity = 5,
                            Price = 17.99m
                        }
                    }
                }
            }
        }
    };
}