// See https://aka.ms/new-console-template for more information


using System.Diagnostics;
using BenchmarkDotNet.Running;
using CustomerDemo.Benchmarks;
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
        RunDemo();
        break;
    case "2":
        RunPerformanceComparison();
        break;
    case "3":
        BenchmarkRunner.Run<FileSerializationBenchmarks>();
        break;
    case "4":
        return;
    default:
        Console.WriteLine("Invalid option");
        break;}


Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();


static void RunDemo()
    {
        var customers = CreateSampleData();

        Console.WriteLine("\n### REFLECTION APPROACH ###");
        var reflectionSerializer = new ReflectionFileSerializer();
        var reflectionOutput = reflectionSerializer.Serialize(customers);
        Console.WriteLine(reflectionOutput);
        Console.WriteLine(new string('=', 80));
        Console.WriteLine();

        Console.WriteLine("### SOURCE GENERATOR APPROACH ###");
        var generatedSerializer = new GeneratedFileSerializer();
        var generatedOutput = generatedSerializer.Serialize(customers);
        Console.WriteLine(generatedOutput);
        Console.WriteLine(new string('=', 80));
        Console.WriteLine();

        if (reflectionOutput == generatedOutput)
        {
            Console.WriteLine("✓ Outputs match perfectly!");
        }
        else
        {
            Console.WriteLine("✗ Outputs differ!");
        }
    }

    static void RunPerformanceComparison()
    {
        Console.WriteLine("\n=== Performance Comparison ===");
        var customers = CreateSampleData();
        const int iterations = 10_000;

        var reflectionSerializer = new ReflectionFileSerializer();
        var generatedSerializer = new GeneratedFileSerializer();

        // Warm up
        _ = reflectionSerializer.Serialize(customers);
        _ = generatedSerializer.Serialize(customers);

        // Test reflection
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            _ = reflectionSerializer.Serialize(customers);
        }
        sw.Stop();
        var reflectionTime = sw.Elapsed.TotalMilliseconds;

        // Test source generator
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            _ = generatedSerializer.Serialize(customers);
        }
        sw.Stop();
        var generatorTime = sw.Elapsed.TotalMilliseconds;

        Console.WriteLine($"Reflection approach:        {reflectionTime,8:F2}ms ({iterations:N0} iterations)");
        Console.WriteLine($"Source generator approach:  {generatorTime,8:F2}ms ({iterations:N0} iterations)");
        Console.WriteLine($"Speed improvement:          {reflectionTime / generatorTime,8:F2}x faster");
        Console.WriteLine($"Time saved per call:        {(reflectionTime - generatorTime) / iterations * 1000,8:F4}μs");
    }

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