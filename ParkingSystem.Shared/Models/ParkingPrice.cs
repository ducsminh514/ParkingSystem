// Thêm vào ParkingSystem.Server/Models/ hoặc ParkingSystem.Shared/Models/

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkingSystem.Shared.Models
{
    public class ParkingPrice
    {
        [Key]
        public Guid PriceId { get; set; }

        [Required]
        [MaxLength(50)]
        public string VehicleType { get; set; } = string.Empty; // "Xe máy", "Ô tô", "Xe đạp", "Xe tải"

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal PricePerHour { get; set; } // Giá theo giờ (VNĐ)

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }
    }
}