# Changelog

## [v2.0.0]

- Breaking: dropped target framework support below .NET 8.
- Breaking: upgraded the MongoDB driver dependency from 2.x to 3.11.0.
- Updated MongoDB.Driver to 3.11.0.
- Limited supported target frameworks to .NET 8, .NET 9, and .NET 10.
- Restored fork IQueryable projection fixes on top of the updated main codebase.
- Migrated LINQ query translation to the MongoDB Driver 3 LINQ3 internals.
- Replaced the System.Linq.Async dependency with internal async enumerable helpers.
- Added NuGet release automation with changelog and `.release` validation.
