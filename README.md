EntityFrameworkCore.Sqlite.HierarchyId
========================================

Adds hierarchyid support to the SQL Server EF Core provider. This is a work in progress project and not ready for production, or even dev/test.

Installation
------------

The latest version is available on [NuGet](https://www.nuget.org/packages/Mapache.EntityFrameworkCore.Sqlite.HierarchyId/).

```sh
dotnet add package Mapache.EntityFrameworkCore.Sqlite.HierarchyId --version 0.0.1
```

Compatibility
-------------

The following table show which version of this library to use with which version of EF Core.

| EF Core | Version to use  |
| ------- | --------------- |
| 7.0     | 0.0.1           |

I have not tested this in EF Core 6.0 or 5.0.


Usage
-----

Enable hierarchyid support by calling UseHierarchyId inside UseSliteServer. UseSliteServer is is typically called inside `Startup.ConfigureServices` or `OnConfiguring` of your DbContext type.

```cs
options.UseSliteServer(
    connectionString,
    x => x.UseHierarchyId());
```

**Currently it only supports the method: IsDescendantOf, as a POC**

All the next information is untrue, it is not supported yet.

Add `HierarchyId` properties to your entity types.

```cs
class Node
{
    public HierarchyId Id { get; set; }
    public string Name { get; set; }
}
```

Insert data.

```cs
dbContext.AddRange(
    new Node { Id = HierarchyId.GetRoot(), Name = "Animals" },
    new Node { Id = HierarchyId.Parse("/1/"), Name = "Felines" },
    new Node { Id = HierarchyId.Parse("/1/1/"), Name = "Cats" });
dbContext.SaveChanges();
```

Query.

```cs
var thirdGeneration = from p in dbContext.Node
                      where p.Id.GetLevel() == 2
                      select p;
```

See also
--------

* [Hierarchical Data (SQL Server)](https://docs.microsoft.com/sql/relational-databases/hierarchical-data-sql-server)
* [Entity Framework documentation](https://docs.microsoft.com/ef/)

Based in the work of [EFCore.SqlServer.HierarchyId](https://github.com/efcore/EFCore.SqlServer.HierarchyId).
