using SqliteDbContext.Context;
using SqliteDbContext.Helpers;
using SqliteDbContextLib.Tests.Tests;
using SqliteDbContextLibTests.Context;
using SqliteDbContextLibTests.Entities;

namespace SqliteDbContextLibTests.Tests
{
    internal class RelationalTests : TestBase
    {
        private SqliteDbContext<EntityProjectContext> context;
        private EntityProjectContext ctx;

        private static Random random = new Random();

        [SetUp]
        public void Setup()
        {
            context = new SqliteDbContext<EntityProjectContext>();
            base.RegisterDbContextDependencies(context);
            ctx = context.Context;
        }

        [Test]
        public void DisplayDependencies()
        {
            var dependencyResolver = new DependencyResolver(ctx);
            Console.WriteLine(string.Join("\n", dependencyResolver.GetDependencyOrder().Select(x => x.Name)));
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
        public void Table1_CustomGenerateTest()
        {
            int total = 50;
            Table1Test(total);
            Assert.AreEqual(total, ctx.Table1.Count());

            var item = context.GenerateEntity<Table1>(table => { table.Col1_PK = 51; table.Col2 = "Random"; table.Col3 = int.MinValue; });
            Assert.AreEqual(total + 1, ctx.Table1.Count());
            Assert.AreEqual(total + 1, item.Col1_PK);
            Assert.AreEqual("Random", item.Col2);
            Assert.AreEqual(int.MinValue, item.Col3);
        }

        [Test]
        public void Table1_CustomGenerate_AutoGenerateTest()
        {
            int total = 50;
            Table1_CustomGenerateTest();
            Assert.AreEqual(total + 1, ctx.Table1.Count());

            var item = context.GenerateEntity<Table1>();

            Assert.AreEqual(total + 2, ctx.Table1.Count());
            Assert.AreEqual(total + 2, item.Col1_PK);
        }

        [Test]
        public void Table1_HighPK_AutoGenerateTest()
        {
            int total = 50;
            //Table1Test(total);
            Table2Test(total/2); //for table 2 test
            Assert.AreEqual(total, ctx.Table1.Count());
            var id = 23859;

            var item = context.GenerateEntity<Table1>(table => table.Col1_PK = id);

            Assert.AreEqual(total + 1, ctx.Table1.Count());
            Assert.AreEqual(id, item.Col1_PK);

            item = context.GenerateEntity<Table1>();
            Assert.AreEqual(total + 2, ctx.Table1.Count());
            Assert.AreEqual(total + 1, item.Col1_PK);
        }

        [Test]
        public void Table2_HighPK_AutoGenerateTest()
        {
            int total = 50;
            int table2Total = total / 2;
            int table1Total = total + 2;
            Table1_HighPK_AutoGenerateTest();
            Assert.AreEqual(table1Total, ctx.Table1.Count());
            Assert.AreEqual(table2Total, ctx.Table2.Count());
        }
    }
}
