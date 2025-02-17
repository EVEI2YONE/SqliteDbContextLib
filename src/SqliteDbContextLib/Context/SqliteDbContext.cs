using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SQLite;
using SqliteDbContext.Extensions;
using SqliteDbContext.Generator;
using SqliteDbContext.Interfaces;
using SqliteDbContext.Strategies;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace SqliteDbContext.Context
{
    public class SqliteDbContext<T> where T : DbContext
    {
        private BogusGenerator bogus;
        private T? context;
        private IDictionary<Type, Delegate> postDependencyResolvers = new Dictionary<Type, Delegate>();
        public T? Context => context;
        private SqliteConnection _connection;
        public DbContextOptions<T> Options => _options;
        private DbContextOptions<T> _options;
        public IDependencyResolver DependencyResolver { get; private set; }

        public SqliteDbContext(string? DbInstanceName = null, SqliteConnection? conn = null)
        {
            _connection = CreateConnection(DbInstanceName, conn);
            DependencyResolver = new DependencyResolver(context);
            bogus = new BogusGenerator(context, DependencyResolver);
        }

        private SqliteConnection CreateConnection(string? dbIntanceName, SqliteConnection? conn)
        {
            dbIntanceName = dbIntanceName ?? Guid.NewGuid().ToString();
            if(conn == null)
            {
                var config = new SqliteConnectionStringBuilder { DataSource = $"{dbIntanceName}:memory:", Mode = SqliteOpenMode.Memory, Cache = SqliteCacheMode.Shared };
                conn = new SqliteConnection(config.ToString());
            }

            if(conn.State != System.Data.ConnectionState.Open)
            {
                conn.Open();
            }

            _options = new DbContextOptionsBuilder<T>()
                .UseSqlite(conn)
                .Options;

            context = (T?)Activator.CreateInstance(typeof(T), _options);
            context?.Database.EnsureDeleted();
            context?.Database.EnsureCreated();
            return conn;
        }

        public void RegisterKeyAssignment<E>(Action<E, IKeySeeder, T> dependencyActionResolver) where E : class
            => postDependencyResolvers.TryAdd(typeof(E), dependencyActionResolver);

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
                //assumes all keys are untouched
                if (entity.GetKeys().Any(x => x.ToString() == "-1" || x.ToString() == null))
                {
                    //validation that all keys are untouched or are warns user that keys are incorrectly assigned
                    if (!entity.GetKeys().All(x => x.ToString() == "-1" || x.ToString() == null))
                        throw new Exception($"Didn't update all keys required to override autogeneration");
                    do //if entity is found, then it was generated ahead of time - skip and generate next valid entity
                    {
                        bogus.ApplyDependencyAction<E,T>(entity, (Action<E, IKeySeeder, T>)postDependencyResolvers[type], context);
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

        public void CloseConnection()
        {
            _connection?.Close();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void CloseAllConnections()
        {
            SQLiteAsyncConnection.ResetPool();
        }

        public T CreateDbContext()
        {
            var args = new object[] { _options };
            return Activator.CreateInstance(typeof(T), args) as T;
        }
    }
}