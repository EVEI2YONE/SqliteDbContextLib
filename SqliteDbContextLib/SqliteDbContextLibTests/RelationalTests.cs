using DbFirstTestProject.DataLayer.Context;
using DbFirstTestProject.DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using SqliteDbContextLib;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContextLibTests
{
    internal class RelationalTests
    {
        private SqliteDbContext<EntityProjectContext> context;
        private EntityProjectContext ctx;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            SqliteDbContext<EntityProjectContext>.RegisterPostDependencyResolver<Table1>((table, seeder) =>
            {
                table.Col1_PK = seeder.IncrementKeys<Table1>().First();
            });

            SqliteDbContext<EntityProjectContext>.RegisterPostDependencyResolver<Table2>((table, seeder) =>
            {
                table.Col1_PK = (int)seeder.IncrementKeys<Table2>().First();
                table.Col2_FK = seeder.GetRandomKeys<Table1>().First();
            });

            SqliteDbContext<EntityProjectContext>.RegisterPostDependencyResolver<Table3>((table, seeder) =>
            {
                var query = ctx.Table2.Select(x => new object[] { x.Table1.Col1_PK, x.Col1_PK });
                var keys = seeder.GetUniqueRandomKeys<Table3>(ctx, query);
                table.Col1_PKFK = (long) keys.First();
                table.Col2_FK = (int) keys.Last();
            });

            SqliteDbContext<EntityProjectContext>.RegisterPostDependencyResolver<Table4>((table, seeder) =>
            {
                var query = ctx.Table3.Select(x => new object[] { x.Table1.Col1_PK, x.Table2.Col1_PK, x.Col1_PKFK, x.Col2_FK });

                var keys = seeder.GetUniqueRandomKeys<Table4>(ctx, query);
                table.Col1_T1PKFK = (long) keys.GetValue(0);
                table.Col2_T2PKFK = (int) keys.GetValue(1);
                table.Col3_T3PKFK_PKFK = (long) keys.GetValue(2);
                table.Col4_T3PKFK_FK = (int) keys.GetValue(3);
            });
        }

        private static Random random = new Random();

        [SetUp]
        public void Setup()
        {
            context = new SqliteDbContext<EntityProjectContext>();
            ctx = context.Context;
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        [TestCase(10)]
        public void Table1Test(int total)
        {
            var items = context.GenerateEntities<Table1>(total, null);
            Assert.IsNotNull(items);
            Assert.AreEqual(total, items.Count());
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        [TestCase(10)]
        public void Table2Test(int total)
        {
            Table1Test(total * 2);
            var items = context.GenerateEntities<Table2> (total, null);
            Assert.IsNotNull(items);
            Assert.AreEqual(total, items.Count());
            Assert.AreEqual(total*2, context.Context.Table1.Count());
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        [TestCase(10)]
        public void Table3Test(int total)
        {
            Table2Test(total * 2);
            var items = context.GenerateEntities<Table3>(total, null);
            Assert.IsNotNull(items);
            Assert.AreEqual(total, items.Count());
            Assert.AreEqual(total * 2, context.Context.Table2.Count());
            Assert.AreEqual(total * 2 * 2, context.Context.Table1.Count());
        }
        
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        [TestCase(10)]
        public void Table4Test(int total)
        {
            Table3Test(total * 2);
            var items = context.GenerateEntities<Table4>(total, null);
            Assert.IsNotNull(items);
            Assert.AreEqual(total, items.Count());
            Assert.AreEqual(total * 2, context.Context.Table3.Count());
            Assert.AreEqual(total * 2 * 2, context.Context.Table2.Count());
            Assert.AreEqual(total * 2 * 2 * 2, context.Context.Table1.Count());
        }
    }
}
