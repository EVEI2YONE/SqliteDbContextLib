using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace SqliteDbContextLib
{
    public class SqliteDbContext<T> where T : DbContext
    {
        private BogusGenerator bogus;
        private T? context;
        private static IDictionary<Type, Delegate> postDependencyResolvers = new Dictionary<Type, Delegate>();
        public T? Context { get { return context; } }

        public SqliteDbContext(string? DbInstanceName = null)
        {
            CreateConnection(DbInstanceName);
            bogus = new BogusGenerator(context);
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
                list.Add(GenerateEntity(initializeAction));
            }
            return list;
        }

        public E GenerateEntity<E>(Action<E>? initializeAction = null) where E : class
        {
            var type = typeof(E);
            if (!postDependencyResolvers.ContainsKey(type))
                throw new Exception($"Must have registered dependency resolver for {type.Name} prior to saving");
            var entity = bogus.Generate<E>();
            bogus.RemoveGeneratedReferences(entity);
            bogus.ClearKeys(entity);
            bogus.ApplyInitializingAction(entity, initializeAction);
            var search = context?.Set<E>()?.Find(entity.GetKeys());
            if (search == null)
            {
                if (entity.GetKeys().Any(x => x.ToString() == "-1" || x.ToString() == null))
                {
                    do //if not null, then it was generated ahead of time - skip and generate next valid entity
                    {
                        bogus.ApplyDependencyAction(entity, (Action<E, IKeySeeder>)postDependencyResolvers[type]);
                        search = context?.Set<E>()?.Find(entity.GetKeys());
                    } while (search != null);
                }
                else //all keys must be initialized in order to override autogeneration - assumes user will handle dependencies outside of what is provided
                {
                    bogus.ApplyInitializingAction(entity, initializeAction);
                }
                search = context?.Set<E>()?.Find(entity.GetKeys());
                context?.Add(entity);
            } //all keys match and found existing item
            else
            {
                bogus.ApplyInitializingAction(search, initializeAction);
            }
            context?.SaveChanges();
            return entity;
        }
    }
}