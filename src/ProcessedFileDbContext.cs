using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ImageResizerAPI;

public class ProcessedFile
{
    [Key]
    public int Id { get; set; }
    public DateTime DateProcessed { get; set; }
    public string UserId { get; set; } = string.Empty;
}

public class ProcessedFileDbContext : DbContext
{
    public ProcessedFileDbContext(DbContextOptions<ProcessedFileDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProcessedFile> ProcessedFiles { get; set; }
}
