using AutoPopulate;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SQLite;
using SqliteDbContext.Extensions;
using SqliteDbContext.Generator;
using SqliteDbContext.Interfaces;
using SqliteDbContext.Strategies;
using System.Data.Common;
using System.Runtime.CompilerServices;
using static System.Formats.Asn1.AsnWriter;

namespace SqliteDbContext.Context
{
    /// <summary>
    /// A wrapper class that encapsulates a DbContext (of type T) to simulate an in-memory DbContext.
    /// Developers work through this wrapper to generate entities and maintain referential integrity.
    /// </summary>
    public class SqliteDbContext<T> where T : DbContext
    {
        public T Context { get; private set; }
        public IDependencyResolver DependencyResolver { get; }
        public IKeySeeder KeySeeder { get; }
        public BogusGenerator BogusGenerator { get; }
        public IEntityGenerator EntityGenerator { get; }
        private SqliteConnection _connection;
        public DbContextOptions<T> Options { get; private set; }

        public SqliteDbContext(string? DbInstanceName = null, SqliteConnection? conn = null)
        {
            _connection = CreateConnection(DbInstanceName, conn);
            DependencyResolver = new DependencyResolver(Context);
            EntityGenerator = new FakeEntityGenerator();
            KeySeeder = new KeySeeder(Context, DependencyResolver, EntityGenerator);
            BogusGenerator = new BogusGenerator(DependencyResolver, KeySeeder, EntityGenerator);
        }

        private DbSet<TEntity> Set<TEntity>() where TEntity : class => Context.Set<TEntity>();

        private SqliteConnection CreateConnection(string? dbIntanceName, SqliteConnection? conn)
        {
            dbIntanceName = dbIntanceName ?? Guid.NewGuid().ToString();
            if (conn == null)
            {
                var config = new SqliteConnectionStringBuilder { DataSource = $"{dbIntanceName}:memory:", Mode = SqliteOpenMode.Memory, Cache = SqliteCacheMode.Shared };
                conn = new SqliteConnection(config.ToString());
            }

            if (conn.State != System.Data.ConnectionState.Open)
            {
                conn.Open();
            }

            Options = new DbContextOptionsBuilder<T>()
                .UseSqlite(conn)
                .Options;

            Context = (T?)Activator.CreateInstance(typeof(T), Options);
            Context?.Database.EnsureDeleted();
            Context?.Database.EnsureCreated();
            return conn;
        }

        /// <summary>
        /// Generates a specified quantity of fake entities of type TEntity.
        /// An optional initialization action allows further customization.
        /// </summary>
        public IEnumerable<TEntity> GenerateEntities<TEntity>(int quantity, Action<TEntity> initAction = null) where TEntity : class, new()
        {
            var entities = new List<TEntity>();
            for (int i = 0; i < quantity; i++)
            {
                var entity = GenerateEntity<TEntity>(initAction);
                entities.Add(entity);
            }
            return entities;
        }

        public TEntity GenerateEntity<TEntity>(Action<TEntity> initAction = null) where TEntity : class, new()
        {
            var entity = BogusGenerator.GenerateFake<TEntity>();
            entity = BogusGenerator.RemoveNavigationProperties(entity);
            initAction?.Invoke(entity);
            // Call the new KeySeeder methods with a recursionDepth parameter.
            KeySeeder.ClearKeyProperties(entity, 0);
            KeySeeder.AssignKeys(entity, 0);
            Set<TEntity>().Add(entity);
            SaveChanges();
            return entity;
        }

        public int SaveChanges() => Context.SaveChanges();

        /// <summary>
        /// Resolve some issues with the SQLite connection not closing properly with Files as DB source.
        /// </summary>
        public void CloseConnection()
        {
            _connection?.Close();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// Additional CloseConnection step to force threads to release lock on file Sqlite sources.
        /// </summary>
        public void CloseAllConnections()
        {
            SQLiteAsyncConnection.ResetPool();
        }

        /// <summary>
        /// Creates a new DbContext with shared connection and options to persist data.
        /// </summary>
        /// <returns></returns>
        public T CopyDbContext()
        {
            var args = new object[] { Options };
            return Activator.CreateInstance(typeof(T), args) as T;
        }
    }
}