using SmoothBrainDevelopers.DataLayer.Test.Context;
using SmoothBrainDevelopers.DataLayer.Test.Domain;
using SqliteDbContext.Context;
using SqliteDbContext.Helpers;
using SqliteDbContextLib.Tests.Tests;

namespace SqliteDbContextLibTests.Tests
{
    internal class RelationalTests : TestBase
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
            Console.WriteLine(string.Join("\n", dependencyResolver.GetDependencyOrder().Select(x => x.Name)));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        [TestCase(10)]
        public void GenerateCustomerTest(int total)
        {
            var customers = context.GenerateEntities<Customer>(total);
            Assert.IsNotNull(customers);
            Assert.AreEqual(total, customers.Count());
        }

        [TestCase(2)]
        public void GenerateRegionsTest(int total)
        {
            var regions = context.GenerateEntities<Region>(total);
            Assert.IsNotNull(regions);
            Assert.AreEqual(total, regions.Count());
        }


        [TestCase(2)]
        [TestCase(5)]
        [TestCase(20)]
        [TestCase(50)]
        public void GenerateProductsTest(int total)
        {
            var products = context.GenerateEntities<Product>(total);
            Assert.IsNotNull(products);
            Assert.AreEqual(total, products.Count());
        }

        [TestCase(1, 2)]
        [TestCase(2, 2)]
        [TestCase(5, 2)]
        public void GenerateStoresTest(int totalStores, int totalRegions)
        {
            //generate 2 regions
            GenerateRegionsTest(totalRegions);
            var stores = context.GenerateEntities<Store>(totalStores);
            Assert.IsNotNull(stores);
            Assert.AreEqual(totalStores, stores.Count());
            //ensure same created regions used
            Assert.AreEqual(totalRegions, context.Context.Regions.Count());
        }

        [TestCase(5, 4, 2)]
        [TestCase(10, 6, 2)]
        [TestCase(20, 10, 2)]
        public void GenerateSalesTest(int totalSales, int totalStores, int totalRegions)
        {
            GenerateStoresTest(totalStores, totalRegions);
            var sales = context.GenerateEntities<Sale>(totalSales);
            Assert.IsNotNull(sales);
            Assert.AreEqual(totalSales, sales.Count());
            Assert.AreEqual(totalStores, context.Context.Stores.Count());
            Assert.AreEqual(totalRegions, context.Context.Regions.Count());
        }

        [TestCase(10, 5, 20)]
        [TestCase(100, 10, 200)]
        [TestCase(1000, 200, 2000)]
        [TestCase(5000, 500, 5000)]
        public void GeneratePurchasesTest(int totalProducts, int totalCustomers, int totalPurchases)
        {
            GenerateProductsTest(totalProducts);
            GenerateCustomerTest(totalCustomers);
            GenerateSalesTest(10, 5, 2);
            try
            {
                var customers = context.Context.Customers.ToList();
                var stores = context.Context.Stores.ToList();
                var products = context.Context.Products.ToList();

                var purchases = context.GenerateEntities<Purchase>(totalPurchases);
                Assert.IsNotNull(purchases);
                Assert.AreEqual(totalPurchases, purchases.Count());
                Assert.AreEqual(totalProducts, context.Context.Products.Count());
                Assert.AreEqual(totalCustomers, context.Context.Customers.Count());
                Assert.AreEqual(10, context.Context.Sales.Count());
                Assert.AreEqual(5, context.Context.Stores.Count());
                Assert.AreEqual(2, context.Context.Regions.Count());
            }
            catch(Exception ex)
            {

            }
        }

        

        //[Test]
        //public void Table4_InitializeTest() 
        //{
        //    int total = 5;
        //    Table4Test(total);

        //    var count = ctx.Table4.Count();

        //    var item = context.GenerateEntity<Table4>(table => { table.Col5_Value = "Random"; table.Col6_Extra = int.MinValue; });
        //    Assert.IsNotNull(item);
        //    Assert.AreEqual("Random", item.Col5_Value);
        //    Assert.AreEqual(int.MinValue, item.Col6_Extra);
        //    Assert.AreEqual(count+1, ctx.Table4.Count());

        //    var search = ctx.Table4.Find(item.Table1Id, item.Table2Id, item.Table3Id);
        //    Assert.IsNotNull(search);
        //    Assert.AreEqual(item, search);
        //}

        //[Test]
        //public void Table1_UpdateTest()
        //{
        //    int total = 50;
        //    Table1Test(total);
        //    Assert.AreEqual(total, ctx.Table1.Count());

        //    var item = context.GenerateEntity<Table1>(table => { table.Table1Id = 1; table.Col2 = "Random"; table.Col3 = int.MinValue; });
        //    Assert.AreEqual(total, ctx.Table1.Count());
        //    Assert.AreEqual(1, item.Table1Id);
        //    Assert.AreEqual("Random", item.Col2);
        //    Assert.AreEqual(int.MinValue, item.Col3);
        //}

        [Test]
        public void Table1_CustomGenerateTest()
        {
            int total = 50;
            GenerateCustomerTest(total);
            Assert.AreEqual(total, ctx.Customers.Count());

            var item = context.GenerateEntity<Customer>(table => { table.CustomerId = 51; table.Name = "Random"; });
            Assert.AreEqual(total + 1, ctx.Customers.Count());
            Assert.AreEqual(total + 1, item.CustomerId);
            Assert.AreEqual("Random", item.Name);
        }

        [Test]
        public void Table1_CustomGenerate_AutoGenerateTest()
        {
            int total = 50;
            Table1_CustomGenerateTest();
            Assert.AreEqual(total + 1, ctx.Customers.Count());

            var item = context.GenerateEntity<Customer>();

            Assert.AreEqual(total + 2, ctx.Customers.Count());
            Assert.AreEqual(total + 2, item.CustomerId);
        }

        //[Test]
        //public void Table1_HighPK_AutoGenerateTest()
        //{
        //    int total = 50;
        //    //Table1Test(total);
        //    Table2Test(total/2); //for table 2 test
        //    Assert.AreEqual(total, ctx.Table1.Count());
        //    var id = 23859;

        //    var item = context.GenerateEntity<Table1>(table => table.Table1Id = id);

        //    Assert.AreEqual(total + 1, ctx.Table1.Count());
        //    Assert.AreEqual(id, item.Table1Id);

        //    item = context.GenerateEntity<Table1>();
        //    Assert.AreEqual(total + 2, ctx.Table1.Count());
        //    Assert.AreEqual(total + 1, item.Table1Id);
        //}

        //[Test]
        //public void Table2_HighPK_AutoGenerateTest()
        //{
        //    int total = 50;
        //    int table2Total = total / 2;
        //    int table1Total = total + 2;
        //    Table1_HighPK_AutoGenerateTest();
        //    Assert.AreEqual(table1Total, ctx.Table1.Count());
        //    Assert.AreEqual(table2Total, ctx.Table2.Count());
        //}
    }
}
