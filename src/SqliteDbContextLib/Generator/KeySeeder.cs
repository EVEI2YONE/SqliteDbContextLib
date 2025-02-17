using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using SqliteDbContext.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContext.Generator
{
    public interface IKeySeeder
    {
        public IEnumerable<long> GetInitialKeys<T>();
        public void InitializeKeys<T>(params long[] initialValues);
        public void ResetKeys<T>();
        public void ResetAllKeys();
        public void ClearKeys<T>();
        public void ClearAllKeys();
        public IEnumerable<long> IncrementKeys<T>();
        public IEnumerable<long> PeekKeys<T>();
        public void DecrementKeys<T>();
        public IEnumerable<long> GetRandomKeys<T>();
        public List<int> GetUniqueRandomKeys<T>(params string[] keyProperties) where T : class;
    }

    /// <summary>
    /// Keeps track of generated IDs/keys per entity type.
    /// Handles auto-incrementing for numeric keys and retrieval of valid foreign keys.
    /// </summary>
    public class KeySeeder
    {
        private readonly DbContext _context;
        private readonly IDependencyResolver _dependencyResolver;
        private readonly Random _rng;

        // Stores the last assigned key value per entity type and property.
        private readonly ConcurrentDictionary<(Type, string), object> _currentKeys = new ConcurrentDictionary<(Type, string), object>();

        // Delegate that can be overridden by developers to provide custom key fetching logic.
        public Func<Type, string, object> CustomKeyFetcher { get; set; }

        // Flag to indicate whether to prefer existing dependent entities for foreign keys.
        public bool AllowExistingForeignKeys { get; set; } = true;

        public KeySeeder(DbContext context, IDependencyResolver dependencyResolver)
        {
            _context = context;
            _dependencyResolver = dependencyResolver;
            _rng = new Random();
        }

        /// <summary>
        /// Clears the key properties of an entity (using DependencyResolver metadata) so they can be re-assigned.
        /// </summary>
        public void ClearKeyProperties<T>(T entity) where T : class
        {
            var meta = _dependencyResolver.GetEntityMetadata().FirstOrDefault(e => e.EntityType == typeof(T));
            if (meta == null) return;
            foreach (var keyProp in meta.PrimaryKeys)
            {
                var propInfo = typeof(T).GetProperty(keyProp);
                if (propInfo != null && propInfo.CanWrite)
                {
                    // Set default value.
                    propInfo.SetValue(entity, GetDefault(propInfo.PropertyType));
                }
            }
        }

        /// <summary>
        /// Assigns keys to an entity by auto-incrementing numeric keys or fetching valid foreign keys.
        /// </summary>
        public void AssignKeys<T>(T entity) where T : class
        {
            var meta = _dependencyResolver.GetEntityMetadata().FirstOrDefault(e => e.EntityType == typeof(T));
            if (meta == null) return;
            foreach (var keyProp in meta.PrimaryKeys)
            {
                var propInfo = typeof(T).GetProperty(keyProp);
                if (propInfo != null && propInfo.CanWrite)
                {
                    object newKey = null;
                    // Allow custom key fetcher override.
                    if (CustomKeyFetcher != null)
                    {
                        newKey = CustomKeyFetcher(typeof(T), keyProp);
                    }
                    else
                    {
                        newKey = AutoIncrementKey(typeof(T), keyProp, propInfo.PropertyType);
                    }
                    propInfo.SetValue(entity, newKey);
                }
            }
            // For foreign keys, if allowed, assign existing valid keys.
            foreach (var foreign in meta.ForeignKeys)
            {
                foreach (var fkProp in foreign.ForeignKeyProperties)
                {
                    var propInfo = typeof(T).GetProperty(fkProp);
                    if (propInfo != null && propInfo.CanWrite)
                    {
                        // Attempt to fetch a random valid key from the referenced entity.
                        var foreignType = _context.Model.FindEntityType(typeof(T)).GetForeignKeys()
                            .FirstOrDefault(fk => fk.Properties.Any(p => p.Name == fkProp))?.PrincipalEntityType.ClrType;
                        if (foreignType != null && AllowExistingForeignKeys)
                        {
                            var set = GetQueryableForType(_context, foreignType);
                            // Get a random entity's key value.
                            var randomKey = set.Cast<object>().FirstOrDefault();
                            if (randomKey != null)
                            {
                                // Assume the foreign key property matches the primary key of the referenced entity.
                                propInfo.SetValue(entity, GetKeyValue(randomKey));
                                continue;
                            }
                        }
                        // If no valid foreign key exists, assign a new key.
                        propInfo.SetValue(entity, AutoIncrementKey(typeof(T), fkProp, propInfo.PropertyType));
                    }
                }
            }
        }

        private IQueryable GetQueryableForType(DbContext ctx, Type entityType)
        {
            // Get the generic "Set<TEntity>()" method from DbContext and invoke it with the foreignType.
            var method = typeof(DbContext).GetMethod("Set", Type.EmptyTypes);
            var generic = method.MakeGenericMethod(entityType);
            return (IQueryable)generic.Invoke(ctx, null);
        }

        /// <summary>
        /// Auto-increments numeric keys or generates new values for other types.
        /// </summary>
        private object AutoIncrementKey(Type entityType, string propertyName, Type keyType)
        {
            var key = (entityType, propertyName);
            object currentVal = _currentKeys.ContainsKey(key) ? _currentKeys[key] : GetDefault(keyType);
            object newVal;
            if (keyType == typeof(int))
            {
                int curr = currentVal is int i ? i : 0;
                newVal = curr + 1;
            }
            else if (keyType == typeof(long))
            {
                long curr = currentVal is long l ? l : 0;
                newVal = curr + 1L;
            }
            else if (keyType == typeof(Guid))
            {
                newVal = Guid.NewGuid();
            }
            else if (keyType == typeof(string))
            {
                newVal = Guid.NewGuid().ToString();
            }
            else if (keyType == typeof(DateTime))
            {
                newVal = DateTime.UtcNow;
            }
            else
            {
                // Fallback: attempt to increment if possible, otherwise return default.
                newVal = GetDefault(keyType);
            }
            _currentKeys[key] = newVal;
            return newVal;
        }

        /// <summary>
        /// Retrieves the key value from an entity using reflection.
        /// </summary>
        private object GetKeyValue(object entity)
        {
            // Assume the entity has a single primary key.
            var key = _context.Model.FindEntityType(entity.GetType())?.FindPrimaryKey();
            if (key != null && key.Properties.Any())
            {
                var propName = key.Properties.First().Name;
                var prop = entity.GetType().GetProperty(propName);
                return prop?.GetValue(entity);
            }
            return null;
        }

        private object GetDefault(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}