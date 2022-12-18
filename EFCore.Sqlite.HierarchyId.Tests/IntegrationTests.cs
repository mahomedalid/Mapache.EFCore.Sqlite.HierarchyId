using Microsoft.EntityFrameworkCore;

namespace EFCore.Sqlite.HierarchyId.Tests
{
    public class IntegrationTests
    {
        [Fact]
        public async Task HappyPath()
        {
            var dbOptions = new DbContextOptionsBuilder<SampleContext>().UseSqlite("Data Source=tests.db", conf => conf.UseHierarchyId());

            var context = new SampleContext(dbOptions.Options);

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            context.Group.Add(new GroupModel(1, "root") { HierarchyId = Microsoft.EntityFrameworkCore.HierarchyId.Parse("/1/") });

            context.Group.Add(new GroupModel(2, "top fermenting") { HierarchyId = Microsoft.EntityFrameworkCore.HierarchyId.Parse("/1/2/") });
            context.Group.Add(new GroupModel(3, "bottom fermenting") { HierarchyId = Microsoft.EntityFrameworkCore.HierarchyId.Parse("/1/3/") });

            context.Group.Add(new GroupModel(4, "porter") { HierarchyId = Microsoft.EntityFrameworkCore.HierarchyId.Parse("/1/2/4/") });
            context.Group.Add(new GroupModel(5, "wheat") { HierarchyId = Microsoft.EntityFrameworkCore.HierarchyId.Parse("/1/2/5/") });
            context.Group.Add(new GroupModel(6, "ale") { HierarchyId = Microsoft.EntityFrameworkCore.HierarchyId.Parse("/1/2/6/") });
            context.Group.Add(new GroupModel(7, "sweet stout") { HierarchyId = Microsoft.EntityFrameworkCore.HierarchyId.Parse("/1/2/7/") });

            context.Group.Add(new GroupModel(8, "american ale") { HierarchyId = Microsoft.EntityFrameworkCore.HierarchyId.Parse("/1/2/6/8/") });
            context.Group.Add(new GroupModel(9, "oatmeal stout") { HierarchyId = Microsoft.EntityFrameworkCore.HierarchyId.Parse("/1/2/7/9/") });
            context.Group.Add(new GroupModel(10, "imperial stout") { HierarchyId = Microsoft.EntityFrameworkCore.HierarchyId.Parse("/1/2/7/10/") });

            context.Group.Add(new GroupModel(11, "lager") { HierarchyId = Microsoft.EntityFrameworkCore.HierarchyId.Parse("/1/3/11/") });
            context.Group.Add(new GroupModel(12, "vienna") { HierarchyId = Microsoft.EntityFrameworkCore.HierarchyId.Parse("/1/3/12/") });

            await context.SaveChangesAsync();

            var bottomFermenting = await context.Group.SingleAsync(g => g.Name.Equals("bottom fermenting"));

            var forTheBeach = await context.Group.Where(g => g.HierarchyId!.IsDescendantOf(bottomFermenting.HierarchyId)).ToListAsync();

            Assert.True(forTheBeach.Count == 3, "There should be 3 type of bottom fermenting beers, the root, lager and vienna");

            var stout = await context.Group.SingleAsync(g => g.Name.Equals("sweet stout"));

            var oatStout = await context.Group.Where(g => g.HierarchyId!.IsDescendantOf(stout.HierarchyId) && g.Name.Contains("oat")).ToListAsync();

            Assert.True(oatStout.Count == 1, "We are looking for stouts made of oat");
            Assert.Equal("oatmeal stout", oatStout.FirstOrDefault()!.Name);

            context.Database.EnsureDeleted();
        }
    }
}