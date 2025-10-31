using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackgroundWorker.Models
{
    public class Asset
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; }
        public List<Asset> Childrens { get; set; } = new List<Asset>();

        public int? ParentId { get; set; }
    }
}