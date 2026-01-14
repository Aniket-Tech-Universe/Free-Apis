using System.ComponentModel.DataAnnotations;

namespace UnsecuredAPIKeys.Data.Models
{
    /// <summary>
    /// Junction table linking API keys to the models they have access to
    /// </summary>
    public class ApiKeyModel
    {
        [Key]
        public long Id { get; set; }

        public long ApiKeyId { get; set; }
        public virtual APIKey ApiKey { get; set; } = null!;

        public long ProviderModelId { get; set; }
        public virtual ProviderModel ProviderModel { get; set; } = null!;

        // When we discovered this key had access to this model
        public DateTime DiscoveredUTC { get; set; }

        // Last time we verified this key still has access to this model
        public DateTime LastVerifiedUTC { get; set; }

        // Whether the key currently has access (could lose access over time)
        public bool HasAccess { get; set; } = true;
    }
}
