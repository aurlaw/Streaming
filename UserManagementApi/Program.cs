using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using UserManagementApi.Domain;
using UserManagementApi.DTOs;
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


// Configure SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=management.db"));

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

// API Endpoints - Users
var users = app.MapGroup("/api/users")
    .WithTags("Users")
    .WithOpenApi();

users.MapGet("/{encodedId}", GetUserById)
    .WithName("GetUserById")
    .WithSummary("Get a user by ID")
    .Produces<UserResponse>(200)
    .Produces(400)
    .Produces(404);

users.MapPost("/", CreateUser)
    .WithName("CreateUser")
    .WithSummary("Create a new user")
    .Produces<UserResponse>(201)
    .Produces(400)
    .Produces(409);

// API Endpoints - Products
var products = app.MapGroup("/api/products")
    .WithTags("Products")
    .WithOpenApi();

products.MapGet("/", GetAllProducts)
    .WithName("GetAllProducts")
    .WithSummary("Get all products")
    .Produces<IEnumerable<ProductResponse>>(200);

products.MapGet("/{encodedId}", GetProductById)
    .WithName("GetProductById")
    .WithSummary("Get a product by ID")
    .Produces<ProductResponse>(200)
    .Produces(400)
    .Produces(404);

app.Run();



#region User Endpoints

static async Task<IResult> GetUserById(string encodedId, UserService userService, IIdEncoder encoder)
{
    var result = await userService.GetUserByEncodedIdAsync(encodedId)
        .MapAsync(user => UserMapper.ToResponse(user, encoder));
    
    return result switch
    {
        Result<UserResponse, Error>.Success(var response) => 
            Results.Ok(response),
        Result<UserResponse, Error>.Failure(Error.ValidationError(var msg)) =>
            Results.BadRequest(new { error = msg }),
        Result<UserResponse, Error>.Failure(Error.NotFoundError(var msg)) => 
            Results.NotFound(new { error = msg }),
        Result<UserResponse, Error>.Failure(Error.DatabaseError(var msg)) => 
            Results.Problem(msg),
        _ => Results.Problem("An unexpected error occurred")
    };
}


static async Task<IResult> CreateUser(CreateUserRequest request, UserService userService, IIdEncoder encoder)
{
    var result = await userService.CreateUserAsync(request)
        .MapAsync(user => UserMapper.ToResponse(user, encoder));
    
    return result switch
    {
        Result<UserResponse, Error>.Success(var response) => 
            Results.Created($"/api/users/{response.Id}", response),
        Result<UserResponse, Error>.Failure(Error.ValidationError(var msg)) => 
            Results.BadRequest(new { error = msg }),
        Result<UserResponse, Error>.Failure(Error.DuplicateError(var msg)) => 
            Results.Conflict(new { error = msg }),
        Result<UserResponse, Error>.Failure(Error.DatabaseError(var msg)) => 
            Results.Problem(msg),
        _ => Results.Problem("An unexpected error occurred")
    };
}

#endregion

#region Product Endpoints


static async Task<IResult> GetAllProducts(ProductService productService, IIdEncoder encoder)
{
    var result = await productService.GetAllProductsAsync()
        .MapAsync(products => products.Select(p => ProductMapper.ToResponse(p, encoder)));
    
    return result switch
    {
        Result<IEnumerable<ProductResponse>, Error>.Success(var response) => 
            Results.Ok(response),
        Result<IEnumerable<ProductResponse>, Error>.Failure(Error.DatabaseError(var msg)) => 
            Results.Problem(msg),
        _ => Results.Problem("An unexpected error occurred")
    };
}


static async Task<IResult> GetProductById(string encodedId, ProductService productService, IIdEncoder encoder)
{
    var result = await productService.GetProductByEncodedIdAsync(encodedId)
        .MapAsync(product => ProductMapper.ToResponse(product, encoder));
    
    return result switch
    {
        Result<ProductResponse, Error>.Success(var response) => 
            Results.Ok(response),
        Result<ProductResponse, Error>.Failure(Error.ValidationError(var msg)) => 
            Results.BadRequest(new { error = msg }),
        Result<ProductResponse, Error>.Failure(Error.NotFoundError(var msg)) => 
            Results.NotFound(new { error = msg }),
        Result<ProductResponse, Error>.Failure(Error.DatabaseError(var msg)) => 
            Results.Problem(msg),
        _ => Results.Problem("An unexpected error occurred")
    };
}

#endregion

#region Seed Data


static List<ProductEntity> GenerateProducts(int count)
{
    var categories = new[]
    {
        "Laptop", "Desktop", "Monitor", "Keyboard", "Mouse", "Headset",
        "Webcam", "Microphone", "Speaker", "Router", "Switch", "Cable",
        "SSD", "HDD", "RAM", "CPU", "GPU", "Motherboard",
        "Case", "Power Supply", "Fan", "Cooler", "Thermal Paste",
        "USB Drive", "SD Card", "External Drive", "Hub", "Adapter",
        "Dock", "Stand", "Pad", "Wrist Rest", "Chair", "Desk"
    };
    
    var adjectives = new[]
    {
        "Pro", "Ultra", "Premium", "Basic", "Advanced", "Standard",
        "Elite", "Essential", "Deluxe", "Compact", "Wireless", "Mechanical",
        "Ergonomic", "Gaming", "Professional", "Budget", "High-End"
    };
    
    var random = new Random(42); // Fixed seed for consistent data
    var products = new List<ProductEntity>();
    
    for (int i = 1; i <= count; i++)
    {
        var category = categories[random.Next(categories.Length)];
        var adjective = adjectives[random.Next(adjectives.Length)];
        var name = $"{adjective} {category} {i}";
        
        // Generate price between $9.99 and $2999.99
        var price = Math.Round((decimal)(random.NextDouble() * 2990 + 10), 2);
        
        products.Add(new ProductEntity
        {
            Name = name,
            Price = price
        });
    }
    
    return products;
}

#endregion