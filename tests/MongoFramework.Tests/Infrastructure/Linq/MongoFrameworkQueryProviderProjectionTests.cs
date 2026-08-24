using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoFramework.Infrastructure.Linq;
using MongoFramework.Infrastructure.Mapping;
using MongoFramework.Linq;

namespace MongoFramework.Tests.Infrastructure.Linq
{
	[TestClass]
	public class MongoFrameworkQueryProviderProjectionTests
	{
		private class ProjectionModel
		{
			public string Id { get; set; }
			public string Title { get; set; }
			public int Count { get; set; }
		}

		[TestInitialize]
		public void Initialise()
		{
			TestBase.ResetMongoDb();
		}

		[TestMethod]
		public void SelectProjectionCreatesTypedQueryable()
		{
			EntityMapping.RegisterType(typeof(ProjectionModel));
			var provider = new MongoFrameworkQueryProvider<ProjectionModel>(TestConfiguration.GetConnection());
			var queryable = new MongoFrameworkQueryable<ProjectionModel>(provider);

			var projected = queryable.Select(e => new { e.Title, e.Count });

			Assert.AreEqual(typeof(string), projected.ElementType.GetProperty(nameof(ProjectionModel.Title)).PropertyType);
			Assert.AreEqual(typeof(int), projected.ElementType.GetProperty(nameof(ProjectionModel.Count)).PropertyType);
		}

		[TestMethod]
		public void SelectProjectionCanRenderQuery()
		{
			EntityMapping.RegisterType(typeof(ProjectionModel));
			var provider = new MongoFrameworkQueryProvider<ProjectionModel>(TestConfiguration.GetConnection());
			var queryable = new MongoFrameworkQueryable<ProjectionModel>(provider);

			var query = queryable.Select(e => new { e.Title, e.Count }).ToQuery();

			StringAssert.Contains(query, "$project");
			StringAssert.Contains(query, "Title");
			StringAssert.Contains(query, "Count");
		}
	}
}
