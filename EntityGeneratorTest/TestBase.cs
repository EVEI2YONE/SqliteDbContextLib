using DbContextDriverProject;
using DbFirstTestProject.DataLayer.Context;
using DbFirstTestProject.DataLayer.Entities;
using EntityGenerator.Generator;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Dynamic;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;

namespace EntityGeneratorTest
{
    public class Tests
    {
        protected EntityProjectContext context;
        protected DbContextDriver driver;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            DbContextOptionsBuilder<EntityProjectContext> optionsBuilder = new DbContextOptionsBuilder<EntityProjectContext>();
            var connection = new SqliteConnection("Data Source=InMemorySample;Mode=Memory;Cache=Shared");
            connection.Open();
            optionsBuilder.UseSqlite(connection);

            context = new EntityProjectContext(optionsBuilder.Options);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            driver = new DbContextDriver(context);
        }

        static long count = 0;

        [SetUp]
        public void Setup()
        {
            driver.Add(new Table1()
            {
                Col1_PK = count++,
                Col2 = "",
                Col4 = ""
            });
        }

        [Test]
        public void Test()
        {
            //supplying random values based on their types
            //maintain keys and generate based on constraints
            //allow for upserts when generating

            var generator = new BogusGenerator();
            var table3 = generator.Generate<Table3>(x => x.Col2_FK = 1);
            var obj2 = generator.Generate<Table4>(x => x.Col5_Value = "RandomValue", x => x.Table3 = table3);

            Expression<Func<int, int>> e = (x => x * x);
            var val = e.Compile();
            var v = val(5);

            var a = generator.Generate<Table1>(x => { x.Col1_PK = count++; x.Col2 = "3"; x.Col3 = 2; }, x => x.Table3.Add(new Table3()));
        }

        //find keys and populate if needed - based on modelbuilder schema generated from db

        [Test]
        public void Test2()
        {
            var test = new BogusGenerator().Generate();
            var test2 = new DefaultGenerationScheme(new Type[] { });
            test2.ProvidePropertyPrimitiveSchema<Table1>(x => x.Table2);
            var test3 = new BogusGenerator().Generate();

        }
    }
}