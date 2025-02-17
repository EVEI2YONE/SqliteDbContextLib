using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using SqliteDbContext.Interfaces;
using System;
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

    public class KeySeeder : IKeySeeder
    {
        private readonly IDictionary<Type, IDictionary<string, List<long>>> Keys = new Dictionary<Type,  IDictionary<string, List<long>>>();
        private readonly IDictionary<Type, IDictionary<string, long>> InitialKeys = new Dictionary<Type, IDictionary<string, long>>();
        private readonly IDictionary<Type, IDictionary<string, long>> CurrentKeys = new Dictionary<Type, IDictionary<string, long>>();
        private readonly Random random = new Random();

        private readonly DbContext _context;
        private readonly IDependencyResolver _dependencyResolver;

        public KeySeeder(DbContext context, IDependencyResolver dependencyResolver)
        {
            _context = context;
            _dependencyResolver = dependencyResolver;
        }

        private IEnumerable<string> GetKeyPropertyNames<T>()
            => typeof(T).GetProperties().Where(x => x.GetCustomAttribute<KeyAttribute>() != null).Select(x => x.Name);

        public void InitializeKeys<T>(params long[] initialValues)
        {
            Type type = typeof(T);
            if (initialValues == null || initialValues.Length == 0)
            {
                InitializeKeys<T>(GetKeyPropertyNames<T>().Select(x => (long)0).ToArray());
                return;
            }
            //else if (initialValues.Length == 0)
            //    throw new Exception($"{type.Name} expected to key attribute properties with initial values passed");

            var keyPropertyNames = GetKeyPropertyNames<T>();
            if (!keyPropertyNames.Any())
                throw new Exception($"{type.Name} must have at least one {nameof(KeyAttribute)} Attribute");

            InitialKeys.TryAdd(type, new Dictionary<string, long>());
            CurrentKeys.TryAdd(type, new  Dictionary<string, long>());

            for(int i = 0; i < initialValues.Length; i++)
            {
                var initValue = initialValues[i];
                initValue = initValue < 0 ? 0 : initValue;
                var propertyName = keyPropertyNames.ElementAt(i);
                InitialKeys[type].TryAdd(propertyName, initialValues[i]);
                CurrentKeys[type].TryAdd(propertyName, initialValues[i]);
            }
        }

        public void ResetAllKeys()
        {
            Keys.Clear();
            CurrentKeys.Clear();
        }

        public void ResetKeys<T>()
        {
            Type type = typeof(T);
            Keys.Remove(type);
            CurrentKeys.Remove(type);
        }

        public void ClearAllKeys()
        {
            ResetAllKeys();
            InitialKeys.Clear();
        }

        public void ClearKeys<T>()
        {
            ResetKeys<T>();
            InitialKeys.Remove(typeof(T));
        }

        public IEnumerable<long> IncrementKeys<T>()
        {
            UpdateKeys<T>(1);
            return PeekKeys<T>();
        }

        public IEnumerable<long> PeekKeys<T>()
        {
            if (CurrentKeys.TryGetValue(typeof(T), out var keys))
                return keys.ToList().Select(x => x.Value);
            else 
                return new List<long>().AsEnumerable();
        }

        public void DecrementKeys<T>()
        {
            UpdateKeys<T>(-1);
        }

        private void UpdateKeys<T>(long change)
        {
            Type type = typeof(T);

            var keyPropertyNames = GetKeyPropertyNames<T>();
            if (!keyPropertyNames.Any())
                throw new Exception($"{type.Name} must have at least one {nameof(KeyAttribute)} Attribute");

            if (!InitialKeys.ContainsKey(type))
                InitializeKeys<T>(keyPropertyNames.Select(x => (long)0).ToArray());

            for (int i = 0; i < keyPropertyNames.Count(); i++)
            {
                var typeDictionary = CurrentKeys[type];
                var propertyName = keyPropertyNames.ElementAt(i);
                if (!typeDictionary.ContainsKey(propertyName))
                    throw new Exception($"{type.Name}.{propertyName} unexpected property name - check code logic");
                if (typeDictionary[propertyName] + change <= 0)
                    continue;
                typeDictionary[propertyName] = typeDictionary[propertyName] + change;
            }
        }

        public IEnumerable<long> GetInitialKeys<T>()
        {
            var type = typeof(T);
            if (!InitialKeys.ContainsKey(type))
                InitializeKeys<T>();
            var keyPropertyNames = GetKeyPropertyNames<T>();
            return keyPropertyNames.Select(x => InitialKeys[type][x]);
        }

        public IEnumerable<long> GetRandomKeys<T>()
        {
            var keys = PeekKeys<T>();
            if (!keys.Any())
                throw new Exception($"Generate dependent entities first to track generated PKs that can be pulled for a random FK value {typeof(T).Name}");
            var initial = GetInitialKeys<T>();
            var randomKeys = new List<long>();
            for (int i = 0; i < initial.Count(); i++)
            {
                var min = initial.ElementAt(i) <= 0 ? 1 : initial.ElementAt(i);
                var max = keys.ElementAt(i) + 1;
                randomKeys.Add(random.NextInt64(min, max));
            }
            return randomKeys;
        }

        /// <summary>
        /// Retrieves unique random keys using a composite key query.
        /// For example, for an entity with keys "CustomerID", "ProductID", "StoreID".
        /// </summary>
        public List<int> GetUniqueRandomKeys<T>(params string[] keyProperties) where T : class
        {
            Expression<Func<T, object[]>> compositeLambda = _dependencyResolver.GetCompositeKeyLambda<T>(keyProperties);
            var keyValues = _context.Set<T>().Select(compositeLambda).FirstOrDefault();
            if (keyValues != null)
                return keyValues.Select(x => Convert.ToInt32(x)).ToList();
            return new List<int>();
        }

        private string UniqueKey<T>(object[] ids)
            => $"{typeof(T).Name}:{string.Join(":", ids)}";
    }
}