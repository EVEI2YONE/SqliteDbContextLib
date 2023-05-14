using DbFirstTestProject.DataLayer.Context;
using DbFirstTestProject.DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
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

        [Test]
        public void Table1_UpdateTest()
        {
            int total = 50;
            Table1Test(total);
            Assert.AreEqual(total, ctx.Table1.Count());

            var item = context.GenerateEntity<Table1>(table => { table.Col1_PK = 1; table.Col2 = "Random"; table.Col3 = int.MinValue; });
            Assert.AreEqual(total, ctx.Table1.Count());
            Assert.AreEqual(1, item.Col1_PK);
            Assert.AreEqual("Random", item.Col2);
            Assert.AreEqual(int.MinValue, item.Col3);
        }

        [Test]
        public void Table4_InitializeTest() 
        {
            int total = 5;
            Table4Test(total);

            var count = ctx.Table4.Count();

            var item = context.GenerateEntity<Table4>(table => { table.Col5_Value = "Random"; table.Col6_Extra = int.MinValue; });
            Assert.IsNotNull(item);
            Assert.AreEqual("Random", item.Col5_Value);
            Assert.AreEqual(int.MinValue, item.Col6_Extra);
            Assert.AreEqual(count+1, ctx.Table4.Count());

            var search = ctx.Table4.Find(item.Col1_T1PKFK, item.Col2_T2PKFK, item.Col3_T3PKFK_PKFK, item.Col4_T3PKFK_FK);
            Assert.IsNotNull(search);
            Assert.AreEqual(item, search);
        }
    }
}
