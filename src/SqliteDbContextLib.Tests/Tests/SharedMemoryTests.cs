using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Newtonsoft.Json;
using NUnit.Framework;
using SmoothBrainDevelopers.DataLayer.Test.Context;
using SmoothBrainDevelopers.DataLayer.Test.Domain;
using SqliteDbContext.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContextLib.Tests.Tests
{
    class SharedMemoryTests : TestBase
    {
        [Test]
        public void Normal_SharedMemoryTest()
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = "Test" + ":memory:",
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared
            };
            var connStr = builder.ToString();

            var connection = new SqliteConnection(connStr);//"Data Source=:memory:;Cache=Shared");
            connection.Open();

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .Options;

            Customer entry = new Customer() { CustomerId = 0, Name = "Test" };
            using (var context = new TestDbContext(options))
            {
                context.Database.EnsureCreated();
                // ... seed data or perform initial setup if needed
                context.Customers.Add(entry);
                context.SaveChanges();
                var entry2 = context.Customers.Find(entry.CustomerId);
                Assert.IsNotNull(entry2);
                Assert.That(entry2.CustomerId, Is.EqualTo(entry.CustomerId));
                Assert.That(entry2.Name, Is.EqualTo(entry.Name));
            }

            using (var context = new TestDbContext(options))
            {
                // ... perform tests using the same in-memory database.
                var entry2 = context.Customers.Find(entry.CustomerId);
                Assert.IsNotNull(entry2);
                Assert.That(entry2.CustomerId, Is.EqualTo(entry.CustomerId));
                Assert.That(entry2.Name, Is.EqualTo(entry.Name));
            }

            connection.Close();
        }

        [Test]
        public void SqliteDbContext_SharedMemoryTest()
        {
            var dbContext = new SqliteDbContext<TestDbContext>();

            var region = dbContext.GenerateEntity<Region>();
            var store = dbContext.GenerateEntity<Store>();

            using (var context = dbContext.CopyDbContext())
            {
                var region2 = context.Regions.Find(region.RegionId);
                Assert.IsNotNull(region2);
                Assert.That(region2.RegionId, Is.EqualTo(region.RegionId));
                Assert.That(region2.Name, Is.EqualTo(region.Name));

                var store2 = context.Stores.Find(store.StoreId);
                Assert.IsNotNull(store2);
                Assert.That(store2.StoreId, Is.EqualTo(store.StoreId));
                Assert.That(store2.Name, Is.EqualTo(store.Name));
            }
        }
    }
}
