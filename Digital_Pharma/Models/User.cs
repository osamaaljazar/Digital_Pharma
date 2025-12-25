using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Pharma.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; } // الباسورد العادي من الفورم

        
        public string PasswordHash { get; set; } // مشفر

        public string PhoneNumber { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }  // إضافة تاريخ الميلاد

        public bool EmailConfirmed { get; set; } = false;

        public bool TwoFactorEnabled { get; set; } = false;

        public DateTimeOffset? LockoutEnd { get; set; }

        // Navigation property
        public ICollection<Search> Searches { get; set; } = new List<Search>();
    }
}
