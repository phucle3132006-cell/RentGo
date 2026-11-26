using System;
using System.Collections.Generic;
using System.Text;

namespace CarRental.Models.Entities
{
    public class VehicleImage
    {
        public long Id { get; set; }

        public long VehicleId { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public bool IsPrimary { get; set; }

        public DateTime CreatedAt { get; set; }

        public Vehicle Vehicle { get; set; } = null!;
    }
}

