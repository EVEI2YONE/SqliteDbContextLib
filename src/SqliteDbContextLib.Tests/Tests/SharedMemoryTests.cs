using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Newtonsoft.Json;
using NUnit.Framework;
using SqliteDbContext.Context;
using SqliteDbContextLibTests.Context;
using SqliteDbContextLibTests.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContextLib.Tests.Tests
{
    class SharedMemoryTests
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

            var options = new DbContextOptionsBuilder<EntityProjectContext>()
                .UseSqlite(connection)
                .Options;

            Table1 entry = new Table1 { Col1_PK = 1, Col2 = "Test", Col3 = 1, Col4 = "Test" };
            using (var context = new EntityProjectContext(options))
            {
                context.Database.EnsureCreated();
                // ... seed data or perform initial setup if needed
                context.Table1.Add(entry);
                //var user = context.Set<User>().Find(user1.Id);
                //context.Set<User>().Add(user1);
                var entry2 = context.Table1.Find(entry);
                Assert.IsNotNull(entry2);
                Assert.That(entry2.Col1_PK, Is.EqualTo(entry.Col1_PK));
                Assert.That(entry2.Col2, Is.EqualTo(entry.Col2));
                Assert.That(entry2.Col3, Is.EqualTo(entry.Col3));
                Assert.That(entry2.Col4, Is.EqualTo(entry.Col4));
            }

            using (var context = new EntityProjectContext(options))
            {
                // ... perform tests using the same in-memory database.
                var entry2 = context.Table1.Find(entry);
                Assert.IsNotNull(entry2);
                Assert.That(entry2.Col1_PK, Is.EqualTo(entry.Col1_PK));
                Assert.That(entry2.Col2, Is.EqualTo(entry.Col2));
                Assert.That(entry2.Col3, Is.EqualTo(entry.Col3));
                Assert.That(entry2.Col4, Is.EqualTo(entry.Col4));
            }

            connection.Close();
        }

        [Test]
        public void SqliteDbContext_SharedMemoryTest()
        {
            var dbContext = new SqliteDbContext<EntityProjectContext>();
            var entry = dbContext.GenerateEntity<Table2>();

            using (var context = dbContext.CreateDbContext())
            {
                var entry2 = context.Table2.Find(entry.Col1_PK, entry.Col2_FK);
                Assert.IsNotNull(entry2);
                Assert.That(entry2.Col1_PK, Is.EqualTo(entry.Col1_PK));
                Assert.That(entry2.Col2_FK, Is.EqualTo(entry.Col2_FK));
                Assert.That(entry2.Col3_Value, Is.EqualTo(entry.Col3_Value));
                Assert.That(entry2.Col4_Extra, Is.EqualTo(entry.Col4_Extra));
            }
        }
    }
}
