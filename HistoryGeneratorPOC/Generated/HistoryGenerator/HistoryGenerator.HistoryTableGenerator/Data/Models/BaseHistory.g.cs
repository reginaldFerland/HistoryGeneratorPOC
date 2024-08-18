using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Generated.Data.Models;
public abstract class BaseHistory
{
    [Key]
    public int Id { get; set; }
    [Column(TypeName = "jsonb")]
    public string Data { get; set; }
    public DateTime UpdatedAt { get; set; }
}
