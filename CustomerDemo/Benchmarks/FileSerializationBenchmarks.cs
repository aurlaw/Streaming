using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CustomerDemo.Models;
using CustomerDemo.Serialization;

namespace CustomerDemo.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class FileSerializationBenchmarks
{
    private List<Customer> _customers = null!;
    private ReflectionFileSerializer _reflectionSerializer = null!;
    private GeneratedFileSerializer _generatedSerializer = null!;

    [GlobalSetup]
    public void Setup()
    {
        _customers = CreateSampleData();
        _reflectionSerializer = new ReflectionFileSerializer();
        _generatedSerializer = new GeneratedFileSerializer();
    }

    [Benchmark(Baseline = true)]
    public string ReflectionSerialization()
    {
        return _reflectionSerializer.Serialize(_customers);
    }

    [Benchmark]
    public string SourceGeneratorSerialization()
    {
        return _generatedSerializer.Serialize(_customers);
    }

    private List<Customer> CreateSampleData()
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
}