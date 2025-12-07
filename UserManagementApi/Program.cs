using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OpenAI;
using Scalar.AspNetCore;
using UserManagementApi.Domain;
using UserManagementApi.DTOs;
using UserManagementApi.Endpoints;
using UserManagementApi.Extensions;
using UserManagementApi.Infrastructure;
using UserManagementApi.Infrastructure.Entities;
using UserManagementApi.Mappers;
using UserManagementApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add OpenAPI/Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure JSON serialization for AOT
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

// Configure options
builder.Services.Configure<OpenAIOptions>(
    builder.Configuration.GetSection(OpenAIOptions.SectionName));

// Configure Postgres/pgvector
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseVector()));  // Enable pgvector support

// Get OpenAI API key from configuration
var openAIApiKey = builder.Configuration["OpenAI:ApiKey"];
if (string.IsNullOrWhiteSpace(openAIApiKey))
{
    throw new InvalidOperationException(
        "OpenAI API key is not configured. Set it using: dotnet user-secrets set \"OpenAI:ApiKey\" \"your-key\"");
}

var openAIOptions = builder.Configuration
    .GetSection(OpenAIOptions.SectionName)
    .Get<OpenAIOptions>() ?? new OpenAIOptions();

// Create OpenAI client
var openAiClient = new OpenAIClient(openAIApiKey);

// Register IChatClient for OpenAI (chat/reasoning)
var chatClient = openAiClient.GetChatClient(openAIOptions.ChatModel);
builder.Services.AddChatClient(chatClient.AsIChatClient())
    .UseFunctionInvocation() // Enable function calling
    .UseLogging() // Log all interactions
    .UseDistributedCache();

// Register IEmbeddingGenerator for OpenAI (embeddings)
var embeddingClient = openAiClient.GetEmbeddingClient(openAIOptions.EmbeddingModel);
builder.Services.AddEmbeddingGenerator(embeddingClient.AsIEmbeddingGenerator(openAIOptions.EmbeddingDimensions))
    .UseLogging();

// Register ID encoder
builder.Services.AddSingleton<IIdEncoder, IdEncoder>();

// // Configure model binding for encoded IDs
// builder.Services.AddControllers(options =>
// {
//     options.ModelBinderProviders.Insert(0, new EncodedIdModelBinderProvider());
// });

// Register services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<IProductEmbeddingService, ProductEmbeddingService>();
builder.Services.AddScoped<INaturalLanguageSearchService, NaturalLanguageSearchService>(); 
builder.Services.AddScoped<IHybridSearchService, HybridSearchService>();
builder.Services.AddSingleton<IPromptService, PromptService>();
builder.Services.AddScoped<IProductTaggingService, ProductTaggingService>();

// Optional: Add caching
builder.Services.AddDistributedMemoryCache();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Apply any pending migrations
    await db.Database.MigrateAsync();    
    
    // Seed data if no products exist
    if (!await db.Products.AnyAsync())
    {
        var productData = GenerateProducts(1000);
        db.Products.AddRange(productData);
        await db.SaveChangesAsync();
        
        Console.WriteLine("Seeded 1000 products");
    }
}

// Map endpoint groups
app.MapGroup("/api/users")
    .WithTags("Users")
    .WithOpenApi()
    .MapUserEndpoints();

app.MapGroup("/api/products")
    .WithTags("Products")
    .WithOpenApi()
    .MapProductEndpoints();

// Map development endpoints
app.MapDevelopmentEndpoints();

app.MapProductTaggingEndpoints();


app.Run();




#region Seed Data


static List<ProductEntity> GenerateProducts(int count)
{
    var random = new Random(42); // Fixed seed for consistent data
    var products = new List<ProductEntity>();
    
    // Define product templates with realistic data
    var productTemplates = new[]
    {
        // Laptops
        new { 
            Category = "Laptop", 
            Brands = new[] { "Dell", "HP", "Lenovo", "ASUS", "Acer", "MSI" },
            Adjectives = new[] { "Professional", "Gaming", "Business", "Student", "Ultra-Thin", "Performance" },
            Descriptions = new[] {
                "Powerful laptop featuring the latest processor technology, high-speed SSD storage, and exceptional build quality. Perfect for professionals and power users who demand reliability and performance.",
                "High-performance gaming laptop with dedicated graphics card, RGB backlit keyboard, and advanced cooling system. Ideal for gamers and content creators who need serious computing power.",
                "Slim and lightweight design perfect for professionals on the go. Features long battery life, fast charging, and enterprise-grade security features.",
                "Versatile laptop with excellent multitasking capabilities, vibrant display, and all-day battery life. Great for students and everyday computing tasks."
            },
            Tags = new[] { "laptop", "portable", "computing", "work", "productivity" },
            PriceRange = (800m, 2500m)
        },
        // Monitors
        new { 
            Category = "Monitor", 
            Brands = new[] { "Samsung", "LG", "Dell", "ASUS", "BenQ", "ViewSonic" },
            Adjectives = new[] { "4K", "Ultrawide", "Gaming", "Professional", "Curved", "HDR" },
            Descriptions = new[] {
                "Stunning display with vibrant colors, high resolution, and excellent viewing angles. Features multiple connectivity options and ergonomic stand for comfortable viewing.",
                "Professional-grade monitor with accurate color reproduction, factory calibration, and extensive connectivity. Perfect for creative professionals and photographers.",
                "Gaming monitor with ultra-fast refresh rate, adaptive sync technology, and minimal input lag. Designed for competitive gamers who demand the best response times.",
                "Large screen real estate with immersive viewing experience. Ideal for multitasking professionals and content creators who need multiple windows open simultaneously."
            },
            Tags = new[] { "monitor", "display", "screen", "visual", "workspace" },
            PriceRange = (200m, 1200m)
        },
        // Keyboards
        new { 
            Category = "Keyboard", 
            Brands = new[] { "Logitech", "Corsair", "Razer", "SteelSeries", "Keychron", "Ducky" },
            Adjectives = new[] { "Mechanical", "Wireless", "Gaming", "Ergonomic", "Compact", "RGB" },
            Descriptions = new[] {
                "Premium mechanical keyboard with customizable switches, durable construction, and satisfying tactile feedback. Perfect for typing enthusiasts and professionals.",
                "Gaming keyboard with programmable keys, customizable RGB lighting, and dedicated media controls. Built for gamers who demand precision and style.",
                "Wireless keyboard with long battery life, reliable connection, and quiet typing experience. Ideal for clean desk setups and wireless freedom.",
                "Ergonomic design reduces strain during extended typing sessions. Features comfortable key spacing and optional palm rest for all-day comfort."
            },
            Tags = new[] { "keyboard", "mechanical", "typing", "input", "peripheral" },
            PriceRange = (50m, 300m)
        },
        // Mice
        new { 
            Category = "Mouse", 
            Brands = new[] { "Logitech", "Razer", "SteelSeries", "Corsair", "Glorious", "Zowie" },
            Adjectives = new[] { "Gaming", "Wireless", "Ergonomic", "Lightweight", "Precision", "Pro" },
            Descriptions = new[] {
                "Precision gaming mouse with high-DPI sensor, programmable buttons, and ergonomic shape. Perfect for competitive gaming and accurate cursor control.",
                "Wireless mouse with long battery life, smooth tracking, and comfortable grip. Great for productivity and everyday computing tasks.",
                "Lightweight design optimized for fast movements and extended gaming sessions. Features premium sensor and ultra-responsive clicks.",
                "Ergonomic mouse designed to reduce hand strain and improve comfort during long work sessions. Ideal for professionals who spend hours at the computer."
            },
            Tags = new[] { "mouse", "gaming", "peripheral", "input", "precision" },
            PriceRange = (30m, 150m)
        },
        // Headsets
        new { 
            Category = "Headset", 
            Brands = new[] { "SteelSeries", "HyperX", "Logitech", "Razer", "Corsair", "Sennheiser" },
            Adjectives = new[] { "Gaming", "Wireless", "Studio", "Premium", "Surround", "Professional" },
            Descriptions = new[] {
                "Immersive gaming headset with surround sound, noise-canceling microphone, and comfortable ear cushions. Perfect for long gaming sessions and team communication.",
                "Wireless headset with exceptional battery life, crystal-clear audio, and comfortable over-ear design. Great for gaming, music, and conference calls.",
                "Studio-quality audio with precise sound reproduction and comfortable design for extended wear. Ideal for audio professionals and music enthusiasts.",
                "Lightweight headset with clear microphone, soft ear pads, and adjustable headband. Perfect for gamers who value comfort and communication quality."
            },
            Tags = new[] { "headset", "audio", "gaming", "communication", "sound" },
            PriceRange = (60m, 350m)
        },
        // Storage
        new { 
            Category = "Storage", 
            Brands = new[] { "Samsung", "Western Digital", "Seagate", "Crucial", "SanDisk", "Kingston" },
            Adjectives = new[] { "NVMe", "Portable", "High-Speed", "External", "Internal", "Solid State" },
            Descriptions = new[] {
                "Ultra-fast NVMe SSD with exceptional read/write speeds and reliable performance. Perfect for system drives and demanding applications that require quick data access.",
                "Portable external storage with large capacity and USB connectivity. Ideal for backups, media storage, and transferring files between computers.",
                "High-capacity hard drive offering excellent value for mass storage needs. Great for media libraries, backups, and archival storage.",
                "Solid state drive combining speed and reliability. Features advanced error correction and long lifespan for critical data storage."
            },
            Tags = new[] { "storage", "ssd", "hdd", "drive", "data" },
            PriceRange = (50m, 400m)
        },
        // Graphics Cards
        new { 
            Category = "Graphics Card", 
            Brands = new[] { "NVIDIA", "AMD", "ASUS", "MSI", "Gigabyte", "EVGA" },
            Adjectives = new[] { "Gaming", "Professional", "High-End", "Performance", "Ray Tracing", "Overclocked" },
            Descriptions = new[] {
                "Powerful graphics card delivering exceptional gaming performance with support for the latest graphics technologies. Features advanced cooling and overclocking potential.",
                "Professional GPU designed for content creation, 3D rendering, and complex computational tasks. Offers certified drivers and exceptional stability.",
                "High-end graphics solution with ray tracing capabilities, AI-enhanced features, and incredible frame rates. Perfect for enthusiast gamers and 4K gaming.",
                "Mid-range graphics card offering great value and solid performance for 1080p and 1440p gaming. Includes efficient cooling and low power consumption."
            },
            Tags = new[] { "gpu", "graphics", "gaming", "rendering", "performance" },
            PriceRange = (300m, 1500m)
        }
    };
    
    for (int i = 1; i <= count; i++)
    {
        var template = productTemplates[random.Next(productTemplates.Length)];
        var brand = template.Brands[random.Next(template.Brands.Length)];
        var adjective = template.Adjectives[random.Next(template.Adjectives.Length)];
        var description = template.Descriptions[random.Next(template.Descriptions.Length)];
        
        var name = $"{brand} {adjective} {template.Category} {i}";
        var price = Math.Round(
            (decimal)(random.NextDouble() * (double)(template.PriceRange.Item2 - template.PriceRange.Item1)) + template.PriceRange.Item1,
            2);
        
        var stock = random.Next(0, 100);
        var isActive = random.Next(100) > 5; // 95% active
        var rating = random.Next(100) > 10 ? Math.Round((decimal)(random.NextDouble() * 2 + 3), 1) : (decimal?)null; // 90% have ratings, between 3.0-5.0
        var reviewCount = rating.HasValue ? random.Next(5, 500) : 0;
        
        // Generate tags - combine template tags with random additional tags
        var additionalTags = new[] { "popular", "new-arrival", "best-seller", "premium", "budget-friendly" };
        var tagList = template.Tags.ToList();
        if (random.Next(100) > 50) // 50% chance of additional tag
            tagList.Add(additionalTags[random.Next(additionalTags.Length)]);
        var tags = string.Join(",", tagList);
        
        products.Add(new ProductEntity
        {
            Name = name,
            Description = description,
            Category = template.Category,
            Brand = brand,
            Price = price,
            Stock = stock,
            IsActive = isActive,
            Rating = rating,
            ReviewCount = reviewCount,
            Tags = tags
        });
    }
    
    return products;
}
#endregion