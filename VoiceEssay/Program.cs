using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OpenAI;
using Scalar.AspNetCore;
using VoiceEssay.Endpoints;
using VoiceEssay.Infrastructure;

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

// Register IEmbeddingGenerator for OpenAI (embeddings)
var embeddingClient = openAiClient.GetEmbeddingClient(openAIOptions.EmbeddingModel);
builder.Services.AddEmbeddingGenerator(embeddingClient.AsIEmbeddingGenerator(openAIOptions.EmbeddingDimensions))
    .UseLogging();


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
    
    // // Seed data if no products exist
    // if (!await db.Products.AnyAsync())
    // {
    //     var productData = GenerateProducts(1000);
    //     db.Products.AddRange(productData);
    //     await db.SaveChangesAsync();
    //     
    //     Console.WriteLine("Seeded 1000 products");
    // }
}

// Map development endpoints
app.MapDevelopmentEndpoints();


app.MapGet("/", () => "Hello World!");

app.Run();