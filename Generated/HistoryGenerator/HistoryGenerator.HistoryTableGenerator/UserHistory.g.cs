using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HistoryGeneratorPOC.Data.Models;
public partial class UserHistory
{
    [Key]
    public int Id { get; set; }
    [Column(TypeName = "jsonb")]
    public string Data { get; set; }
    public DateTime UpdatedAt { get; set; }
}
