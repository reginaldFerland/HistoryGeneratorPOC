using HistoryGenerator;
using System.ComponentModel.DataAnnotations;

namespace HistoryGeneratorPOC.Data.Models;

[HistoryTable("user_history")]
public class User
{
    [Key]
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
