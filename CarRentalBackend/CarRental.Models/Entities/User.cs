using System;
using System.Collections.Generic;
using System.Text;

using CarRental.Models.Enums;

namespace CarRental.Models.Entities
{
    public class User
    {
        public long Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty; 

        public UserRole Role { get; set; }

        public UserStatus Status { get; set; }
        public Guid? SessionVersion { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
        public ICollection<RentalOrder> RentalOrders { get; set; }
            = new List<RentalOrder>();
    }
}
