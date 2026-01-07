using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComplaintAnalysis.Infrastructure.Data.Entities;

[Table("LabelRules")]
public class LabelRuleEntity
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ConfigurationId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string LabelId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;
    
    public int Priority { get; set; }
    public int Threshold { get; set; }
    
    // JSON arrays stored as strings
    public string Keywords { get; set; } = "[]"; // JSON array
    public string Phrases { get; set; } = "[]"; // JSON array
    public string Regex { get; set; } = "[]"; // JSON array
    public string Exclusions { get; set; } = "[]"; // JSON array
    public string Dominates { get; set; } = "[]"; // JSON array
    public string HardRules { get; set; } = "[]"; // JSON array
    
    // Navigation property
    [ForeignKey("ConfigurationId")]
    public virtual LabelConfigurationEntity? Configuration { get; set; }
}

