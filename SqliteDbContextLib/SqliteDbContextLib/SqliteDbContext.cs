using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace SqliteDbContextLib
{
    public class SqliteDbContext<T> where T : DbContext
    {
        private DependencyResolver resolver;
        private BogusGenerator<T> bogus;
        private IEnumerable<PropertyMetadata> pkProperties;
        private IEnumerable<PropertyMetadata> fkProperties;
        private IEnumerable<Type> keylessEntities;
        private T context;

        public SqliteDbContext(string DbInstanceName = null)
        {
            CreateConnection(DbInstanceName);
            bogus = InitializeBogusGenerator();
        }

        public BogusGenerator<T> InitializeBogusGenerator()
        {
            resolver = new DependencyResolver(context);
            fkProperties = resolver.GetForeignKeyPropertyMetadata();
            pkProperties = resolver.GetPrimaryKeyPropertyMetadata();
            keylessEntities = resolver.GetKeylessEntities();
            return new BogusGenerator<T>(context, pkProperties, fkProperties);
        }

        private void CreateConnection(string dbIntanceName)
        {
            dbIntanceName = dbIntanceName ?? Guid.NewGuid().ToString();
            var config = new SqliteConnectionStringBuilder { DataSource = $"{dbIntanceName}:memory:", Mode = SqliteOpenMode.Memory, Cache = SqliteCacheMode.Shared };
            SqliteConnection connection = new SqliteConnection(config.ToString());
            connection.Open();

            var options = new DbContextOptionsBuilder<T>()
              .UseSqlite(connection)
              .Options;

            context = (T)Activator.CreateInstance(typeof(T), options);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }

        public E Generate<E>(Action<E>? initializeAction = null) where E : class, new()
        {
            var entity = bogus.Generate<E>();
            bogus.ClearKeys(entity);
            bogus.ApplyInitializingAction(entity, initializeAction);
            bogus.PopulateEntityKeys(entity);
            return entity;
        }

        public IEnumerable<Type> GetDependencyOrder()
        {
            return resolver.GetDependencyOrder();
        }
    }
}