using System.ComponentModel.DataAnnotations;

namespace UnsecuredAPIKeys.Data.Models
{
    public class VerificationBatch
    {
        [Key]
        public long Id { get; set; }

        // Instance identifier (hostname, container ID, etc.)
        [Required]
        public required string InstanceId { get; set; }

        // When this batch was locked
        public DateTime LockedAtUTC { get; set; }

        // When the lock expires (e.g., 30 minutes after locking)
        public DateTime LockExpiresAtUTC { get; set; }

        // Status of the batch
        public VerificationBatchStatus Status { get; set; }

        // Range of keys being processed
        public long StartKeyId { get; set; }
        public long EndKeyId { get; set; }

        // Number of keys in this batch
        public int KeyCount { get; set; }

        // When processing started and completed
        public DateTime? ProcessingStartedAtUTC { get; set; }
        public DateTime? ProcessingCompletedAtUTC { get; set; }

        // Results
        public int? ValidKeys { get; set; }
        public int? InvalidKeys { get; set; }
        public int? SkippedKeys { get; set; }
        public int? ErrorKeys { get; set; }

        // Error information if batch failed
        public string? ErrorMessage { get; set; }
    }

    public enum VerificationBatchStatus
    {
        Locked = 0,
        Processing = 1,
        Completed = 2,
        Failed = 3,
        Expired = 4
    }
}
