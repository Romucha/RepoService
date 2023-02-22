using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoService.Models
{
    public class ProductModel
    {
        [Required]
        public string ProductName { get; set; }
        [Required]
        public string ProductVersion { get; set; }
        [Required]
        public Guid ProductCode { get; set; }
        [Required]
        public Guid UpgradeCode { get; set; }
        [Key]
        [Required]
        public Guid PackageCode { get; set; }
        public bool IsX64 { get; set; } = true;
        [Required]
        public string Manufacturer { get; set; }
        public string ARPCONTACT { get; set; }
        public string ARPHELPLINK { get; set; }
        public string ARPURLINFOABOUT { get; set; }
        public string ARPURLUPDATEINFO { get; set; }
        public string ARPHELPTELEPHONE { get; set; } 
    }
}
