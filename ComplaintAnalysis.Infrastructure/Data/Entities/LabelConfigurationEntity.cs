using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComplaintAnalysis.Infrastructure.Data.Entities;

[Table("LabelConfigurations")]
public class LabelConfigurationEntity
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Version { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(10)]
    public string Language { get; set; } = "en";
    
    public double MinConfidenceToEmit { get; set; }
    public int MaxLabelsPerComplaint { get; set; }
    public int NegationWindowTokens { get; set; }
    
    // Severity configuration
    public string HighSentiment { get; set; } = string.Empty; // JSON array as string
    public string HighCriticalTerms { get; set; } = string.Empty; // JSON array as string
    public string HighCriticalLabels { get; set; } = string.Empty; // JSON array as string
    public string MediumSentiment { get; set; } = string.Empty; // JSON array as string
    public string MediumCriticalTerms { get; set; } = string.Empty; // JSON array as string
    public string MediumCriticalLabels { get; set; } = string.Empty; // JSON array as string
    
    // Navigation property
    public virtual ICollection<LabelRuleEntity> Labels { get; set; } = new List<LabelRuleEntity>();
}

