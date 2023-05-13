using Bogus;
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
        public void IncrementKeys<T>();
        public IEnumerable<long> PeekKeys<T>();
        public void DecrementKeys<T>();
        public IEnumerable<long> GetRandomKeys<T>();
    }

    public class KeySeeder : IKeySeeder
    {
        private static readonly IDictionary<Type, IDictionary<string, List<long>>> Keys = new Dictionary<Type,  IDictionary<string, List<long>>>();
        private static readonly IDictionary<Type, IDictionary<string, long>> InitialKeys = new Dictionary<Type, IDictionary<string, long>>();
        private static readonly IDictionary<Type, IDictionary<string, long>> CurrentKeys = new Dictionary<Type, IDictionary<string, long>>();
        private static readonly Random random = new Random();

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

            for(int i = 0; i < initialValues.Length; i++)
            {
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

        public void IncrementKeys<T>()
        {
            UpdateKeys<T>(1);
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
                randomKeys.Add(random.NextInt64(initial.ElementAt(i), keys.ElementAt(i)));
            return randomKeys;
        }
    }
}