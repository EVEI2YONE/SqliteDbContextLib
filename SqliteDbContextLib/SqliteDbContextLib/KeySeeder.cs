using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContextLib
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
        public object[] GetUniqueRandomKeys<T>(DbContext context, IQueryable<object[]> QueryUniqueKeys) where T : class;
    }

    public class KeySeeder : IKeySeeder
    {
        private readonly IDictionary<Type, IDictionary<string, List<long>>> Keys = new Dictionary<Type,  IDictionary<string, List<long>>>();
        private readonly IDictionary<Type, IDictionary<string, long>> InitialKeys = new Dictionary<Type, IDictionary<string, long>>();
        private readonly IDictionary<Type, IDictionary<string, long>> CurrentKeys = new Dictionary<Type, IDictionary<string, long>>();
        private readonly Random random = new Random();

        private IEnumerable<string> GetKeyPropertyNames<T>()
            => typeof(T).GetProperties().Where(x => x.GetCustomAttribute<KeyAttribute>() != null).Select(x => x.Name);

        public void InitializeKeys<T>(params long[] initialValues)
        {
            Type type = typeof(T);
            if (initialValues == null)
            {
                InitializeKeys<T>(GetKeyPropertyNames<T>().Select(x => (long)0).ToArray());
                return;
            }
            else if (initialValues.Length == 0)
                throw new Exception($"{type.Name} expected to key attribute properties with initial values passed");

            var keyPropertyNames = GetKeyPropertyNames<T>();
            if (!keyPropertyNames.Any())
                throw new Exception($"{type.Name} must have at least one {nameof(System.ComponentModel.DataAnnotations.KeyAttribute)} Attribute");

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
                throw new Exception($"{type.Name} must have at least one {nameof(System.ComponentModel.DataAnnotations.KeyAttribute)} Attribute");

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

        public object[] GetUniqueRandomKeys<T>(DbContext context, IQueryable<object[]> QueryUniqueKeys) where T : class
        {
            int attempts = 0;
            var properties = typeof(T).GetProperties().Where(x => x.GetCustomAttribute<KeyAttribute>() != null);
            object[] keySet;
            int index = -1;
            var uniqueKeys = QueryUniqueKeys.ToList();
            do
            {
                if (!uniqueKeys.Any())
                    throw new Exception("Need more entities to produce unique set of keys");
                if (attempts++ > 1000)
                    throw new Exception("Update logic to produce unique key set");
                if (index > -1)
                    uniqueKeys.RemoveAt(index);
                index = random.Next(0, uniqueKeys.Count);
                keySet = uniqueKeys.ElementAt(index);
            } while (context.Set<T>().Find(keySet) != null);
            return keySet;
        }
    }
}