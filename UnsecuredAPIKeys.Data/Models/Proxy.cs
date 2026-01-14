using System;
using System.ComponentModel.DataAnnotations;

namespace UnsecuredAPIKeys.Data.Models
{
    public class Proxy
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string ProxyUrl { get; set; } // Format: "scheme://[user:pass@]host:port", e.g., "http://myuser:mypass@proxy.example.com:8080" or "socks5://proxy.example.com:1080"

        public DateTime LastUsedUTC { get; set; }
    }
}
