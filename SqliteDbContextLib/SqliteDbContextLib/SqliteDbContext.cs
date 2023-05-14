using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace SqliteDbContextLib
{
    public class SqliteDbContext<T> where T : DbContext
    {
        private DependencyResolver resolver;
        private BogusGenerator bogus;
        private IEnumerable<PropertyMetadata> pkProperties;
        private IEnumerable<PropertyMetadata> fkProperties;
        private IEnumerable<Type> keylessEntities;
        private T? context;
        private static IDictionary<Type, Delegate> postDependencyResolvers = new Dictionary<Type, Delegate>();
        public T? Context { get { return context; } }
        public IEnumerable<string> DependencyOrder { get { return resolver?.GetDependencyOrder().Select(x => x.Name) ?? throw new Exception("No types have been registered. Ensure getters are set in context"); } }

        public SqliteDbContext(string? DbInstanceName = null)
        {
            CreateConnection(DbInstanceName);
            resolver = new DependencyResolver(context);
            fkProperties = resolver.GetForeignKeyPropertyMetadata();
            pkProperties = resolver.GetPrimaryKeyPropertyMetadata();
            keylessEntities = resolver.GetKeylessEntities();
            bogus = new BogusGenerator(context, pkProperties, fkProperties);
        }

        private void CreateConnection(string? dbIntanceName)
        {
            dbIntanceName = dbIntanceName ?? Guid.NewGuid().ToString();
            var config = new SqliteConnectionStringBuilder { DataSource = $"{dbIntanceName}:memory:", Mode = SqliteOpenMode.Memory, Cache = SqliteCacheMode.Shared };
            SqliteConnection connection = new SqliteConnection(config.ToString());
            connection.Open();

            var options = new DbContextOptionsBuilder<T>()
              .UseSqlite(connection)
              .Options;

            context = (T?)Activator.CreateInstance(typeof(T), options);
            context?.Database.EnsureDeleted();
            context?.Database.EnsureCreated();
        }

        public static void RegisterPostDependencyResolver<E>(Action<E, IKeySeeder> dependencyActionResolver) where E : class
            => postDependencyResolvers.TryAdd(typeof(E), (Delegate)dependencyActionResolver);

        public List<E> GenerateEntities<E>(int count, Action<E>? initializeAction = null) where E : class
        {
            var list = new List<E>();
            for(int i = 0; i < count; i++)
            {
                list.Add(Generate(initializeAction));
            }
            return list;
        }

        public E Generate<E>(Action<E>? initializeAction = null) where E : class
        {
            var type = typeof(E);
            if (!postDependencyResolvers.ContainsKey(type))
                throw new Exception($"Must have registered dependency resolver for {type.Name} prior to saving");
            var entity = bogus.Generate<E>();
            bogus.RemoveGeneratedReferences(entity);
            bogus.ClearKeys(entity);
            bogus.ApplyDependencyAction(entity, (Action<E, IKeySeeder>) postDependencyResolvers[type]);
            bogus.ApplyInitializingAction(entity, initializeAction);
            context?.Add(entity);
            context?.SaveChanges();
            return entity;
        }
    }
}