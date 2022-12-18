using Microsoft.EntityFrameworkCore;
using System.Data.Entity.Hierarchy;

namespace EFCore.Sqlite.HierarchyId.Tests
{
    public class GroupModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public Microsoft.EntityFrameworkCore.HierarchyId? HierarchyId { get; set; }

        public GroupModel() { }

        public GroupModel(int id, string name)
        {
            this.Id = id;
            this.Name = name;
        }
    }
}