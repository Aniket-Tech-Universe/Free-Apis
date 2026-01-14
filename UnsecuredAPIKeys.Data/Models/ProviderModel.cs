using System.ComponentModel.DataAnnotations;
using UnsecuredAPIKeys.Data.Common;

namespace UnsecuredAPIKeys.Data.Models
{
    /// <summary>
    /// Represents a model offered by an API provider (e.g., GPT-4, Claude, Gemini)
    /// </summary>
    public class ProviderModel
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public ApiTypeEnum ApiType { get; set; }

        [Required]
        [MaxLength(100)]
        public required string ModelId { get; set; } // e.g., "models/gemini-1.5-pro"

        [MaxLength(100)]
        public string? DisplayName { get; set; } // e.g., "Gemini 1.5 Pro"

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? Version { get; set; }

        // Model capabilities
        public long? InputTokenLimit { get; set; }
        public long? OutputTokenLimit { get; set; }

        // JSON array of supported methods (e.g., ["generateContent", "countTokens"])
        public string? SupportedMethods { get; set; }

        // Model parameters
        public float? Temperature { get; set; }
        public float? TopP { get; set; }
        public int? TopK { get; set; }
        public float? MaxTemperature { get; set; }

        // Grouping for display purposes
        [MaxLength(50)]
        public string? ModelGroup { get; set; } // e.g., "Gemini 1.5", "GPT-4", "Claude 3"

        // Metadata
        public bool IsActive { get; set; } = true;
        public bool IsDeprecated { get; set; } = false;
        public DateTime? DeprecatedDate { get; set; }
        public DateTime FirstSeenUTC { get; set; }
        public DateTime LastSeenUTC { get; set; }

        // Navigation properties
        public virtual ICollection<ApiKeyModel> ApiKeyModels { get; set; } = [];
    }
}
