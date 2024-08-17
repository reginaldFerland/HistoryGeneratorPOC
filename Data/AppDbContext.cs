using HistoryGeneratorPOC.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace HistoryGeneratorPOC.Data;

public class AppDbContext : DbContext
{

    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

}