using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Pharma.Models
{
    public class Search
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Drug1Name { get; set; }

        [Required]
        public string Drug2Name { get; set; }

        public bool InteractionFound { get; set; }

        public string ResultMessage { get; set; }

        public DateTime SearchDate { get; set; } = DateTime.Now;

        // ربط البحث بالمستخدم
        public int? UserId { get; set; } // Nullable

        [ForeignKey("UserId")]
        public User? User { get; set; } // Nullable
    }
}
