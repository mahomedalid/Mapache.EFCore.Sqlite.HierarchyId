using Microsoft.EntityFrameworkCore;

namespace EFCore.Sqlite.HierarchyId.Tests
{
    public class SampleContext : DbContext
    {
        public SampleContext(DbContextOptions<SampleContext> options) 
            : base(options)
        {
        }

        public DbSet<GroupModel> Group { get; set; }
    }
}