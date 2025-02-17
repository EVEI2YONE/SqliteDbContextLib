using Microsoft.EntityFrameworkCore;
using SmoothBrainDevelopers.DataLayer.Test.Context;
using SmoothBrainDevelopers.DataLayer.Test.Domain;
using SqliteDbContext.Context;
using SqliteDbContext.Debug;
using SqliteDbContext.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContextLib.Tests.Tests
{
    internal class DebugTests : TestBase
    {
        private SqliteDbContext<TestDbContext> context;
        private TestDbContext ctx;

        private static Random random = new Random();

        [SetUp]
        public void Setup()
        {
            context = new SqliteDbContext<TestDbContext>();
            base.RegisterDbContextDependencies(context);
            ctx = context.Context;
        }

        [Test]
        public void DisplayDependencies()
        {
            var dependencyResolver = new DependencyResolver(ctx);
            //Console.WriteLine(string.Join("\n", dependencyResolver.GetDependencyOrder().Select(x => x.Name)));
            Console.WriteLine(string.Join("\n", dependencyResolver.GetOrderedEntityTypes()));
        }

        [Test]
        public void DbContextDebuggerTest()
        {
            var dbContextDebugger = new DbContextDebugger<TestDbContext>(context);

            var entities = context.GenerateEntities<Purchase>(5);
            var keySeeder = context.KeySeeder;

            dbContextDebugger.DumpKeySeederStatus(keySeeder);
        }
    }
}
