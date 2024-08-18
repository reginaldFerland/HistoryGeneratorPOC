using HistoryGeneratorPOC.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace HistoryGeneratorPOC.Data;
public partial class AppDbContext : DbContext
{
    public DbSet<UserHistory> UserHistorys { get; set; }
}
