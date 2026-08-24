using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoFramework.Infrastructure;
using MongoFramework.Infrastructure.Indexing;
using MongoFramework.Infrastructure.Mapping;
using MongoFramework.Infrastructure.Serialization;

namespace MongoFramework.Tests
{
	[TestClass]
	public abstract class TestBase
	{
		internal static void ResetMongoDb()
		{
			MongoDbDriverHelper.ResetDriver();
			EntityMapping.RemoveAllDefinitions();

			EntityMapping.RemoveAllMappingProcessors();
			EntityMapping.AddMappingProcessors(DefaultMappingProcessors.Processors);

			TypeDiscovery.ClearCache();
			EntityIndexWriter.ClearCache();

			DriverAbstractionRules.ApplyRules();
		}

		protected static void ClearDatabase()
		{
			//Removing the database created for the tests
			var settings = MongoClientSettings.FromConnectionString(TestConfiguration.ConnectionString);
			settings.ServerSelectionTimeout = System.TimeSpan.FromSeconds(2);
			var client = new MongoClient(settings);
			client.DropDatabase(TestConfiguration.GetDatabaseName());
		}

		[TestInitialize]
		public void Initialise()
		{
			ResetMongoDb();
			ClearDatabase();
		}

		[AssemblyCleanup]
		public static void AssemblyCleanup()
		{
			try
			{
				ClearDatabase();
			}
			catch (System.TimeoutException)
			{
			}
			catch (MongoDB.Driver.MongoConnectionException)
			{
			}
		}
	}
}
