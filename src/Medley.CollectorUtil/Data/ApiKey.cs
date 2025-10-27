using System.ComponentModel.DataAnnotations;

namespace Medley.CollectorUtil.Data;

public class ApiKey
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string Key { get; set; } = string.Empty;
    
    public bool IsEnabled { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
