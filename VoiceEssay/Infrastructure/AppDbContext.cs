using Microsoft.EntityFrameworkCore;

namespace VoiceEssay.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Enable pgvector extension
        modelBuilder.HasPostgresExtension("vector");        
        
 
    }
}