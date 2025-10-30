using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace BackgroundWorker
{
    public class DBContext : DbContext
    {
        public DBContext(DbContextOptions options) : base(options)
        {

        }
        public DbSet<CombinationData> Combinations { get; set; }
    }
}
