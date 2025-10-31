using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackgroundWorker.Models
{
    public class CombinationData
    {
        [Key]
        public int Id { get; set; }

        public required string Word { get; set; }

        public required int Count { get; set; }

        public int AssetId { get; set; }
    }
}
