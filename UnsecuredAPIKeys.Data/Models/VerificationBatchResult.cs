using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnsecuredAPIKeys.Data.Models
{
    public class VerificationBatchResult
    {
        [Key]
        public long Id { get; set; }
        public DateTime VerificationDateUTC { get; set; }

        public int BatchSize { get; set; }
        public int ValidKeys { get; set; }
        public int InvalidKeys { get; set; }
        public int SkippedKeys { get; set; }

        public double TimeTakenInMinutes { get; set; }
    }
}
