using HistoryGeneratorPOC.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace HistoryGeneratorPOC.Data;

public partial class AppDbContext : DbContext
{

    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

}