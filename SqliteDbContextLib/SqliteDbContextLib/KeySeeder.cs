using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContextLib
{
    /// <summary>
    /// Index by Expression is based on an instance's property.
    /// Index by string is based on the resulting expression name.
    /// </summary>
    /// <code>
    /// IndexInitialValue<Person>(p => p.Name);
    /// </code>
    /// <example>
    /// "Person.Name" is the resulting key based on TypeName.PropertyName
    /// </example>
    /// <typeparam name="T"></typeparam>
    /// <param name="expression"></param>
    public class KeySeeder
    {
        private IEnumerable<PropertyMetadata> properties;
        private static IDictionary<string, object> initialKeys = new Dictionary<string, object>();
        private static IDictionary<string, object> incrementKeys = new Dictionary<string, object>();
        private static IDictionary<string, List<object>> allKeys = new Dictionary<string, List<object>>();
        private static object _lock = new object();
        private static object _allKeys = new object();

        public static Random Random = new Random();

        public static void ResetKeys()
        {
            lock (_lock)
            {
                incrementKeys.Clear();
                allKeys.Clear();
            }
        }

        public static void ClearKeys()
        {
            lock (_lock)
            {
                initialKeys.Clear();
                incrementKeys.Clear();
                allKeys.Clear();
            }
        }

        public static bool TryGetRandomKey<T>(Expression<Func<T, object>> expression, out object? keyValue)
        {
            var key = GetExpressionKey(expression);
            return TryGetRandomKeyFromName(key, out keyValue);
        }

        public static bool TryGetRandomKeyFromName(string key, out object? keyValue)
        {
            keyValue = null;
            if (!allKeys.ContainsKey(key))
                lock (_allKeys) allKeys.TryAdd(key, new List<object>());
            if (!allKeys.TryGetValue(key, out List<object>? list) && list.Any())
            {
                var start = 0;
                var end = list.Count();
                keyValue = list[Random.Next(start, end)];
                return keyValue != null;
            }
            return false;
        }

        private static object? FetchKeyValue(string key, object value)
        {
            if (allKeys.TryGetValue(key, out List<object> keys))
                return keys.FirstOrDefault(x => (x == null) ? false : x.ToString().Equals(value.ToString(), StringComparison.InvariantCultureIgnoreCase));
            else
                return null;
        }

        public static bool KeyExists<T>(Expression<Func<T, object>> expression, [Required] object value)
        {
            var key = GetExpressionKey(expression);
            return KeyExistsFromName(key, value);
        }

        public static bool KeyExistsFromName(string key, [Required] object value)
        => (value == null) ? false : FetchKeyValue(key, value) != null;

        public static void RemoveKey<T>(Expression<Func<T, object>> expression, object value)
        => RemoveKeyFromName(GetExpressionKey(expression), value);
        public static void RemoveKeyFromName(string key, object value)
        {
            if (value == null)
                return;
            lock (_allKeys)
            {
                if (!allKeys.ContainsKey(key))
                    return;
                var keyRef = allKeys[key].FirstOrDefault(x => (x == null) ? false : x.ToString().Equals(value.ToString(), StringComparison.CurrentCultureIgnoreCase));
                if (keyRef != null)
                    allKeys[key].Remove(keyRef);
            }
        }

        public static void AddGeneratedKey(string key, object value)
        {
            if (value == null)
                return;
            lock (_allKeys)
            {
                if (!allKeys.ContainsKey(key))
                    allKeys.TryAdd(key, new List<object>());
                allKeys[key].Add(value);
            }
        }

        public static object IncrementOrInitialize<T>(Expression<Func<T, object>> expression, object? initialValueOrType = null) where T : class
        {
            var key = GetExpressionKey(expression);
            return IncrementOrInitializeFromName<T>(key, initialValueOrType ?? expression.ReturnType);
        }

        public static object IncrementOrInitializeFromName<T>(string key, object initialValueOrType) where T : class
        {
            if (initialValueOrType == null)
                throw new Exception("Initial value must be supplied or its struct/primitive Type");
            incrementKeys.TryGetValue(key, out object? value);
            lock (_lock)
            {
                if (value == null)
                {
                    initialKeys.TryAdd(key, (initialValueOrType is Type) ? GetInitialValue((Type)initialValueOrType) : initialValueOrType);
                    incrementKeys.TryAdd(key, initialKeys[key]);
                    AddGeneratedKey(key, initialValueOrType);
                }
                else
                {
                    incrementKeys[key] = IncrementValue(incrementKeys[key]);
                    AddGeneratedKey(key, incrementKeys[key]);
                }
                return incrementKeys[key];
            }
        }

        private static object GetInitialValue(Type type)
        {
            if (type == typeof(string))
                return Guid.NewGuid().ToString();
            else if (type == typeof(Guid))
                return Guid.NewGuid();
            else
                return 1;
        }

        private static object IncrementValue(object value)
        {
            var type = value.GetType();
            //maintain precision and based on max value
            if (int.TryParse(value.ToString(), out int result))
            {
                if ((Convert.ToInt64(value)) <= double.MaxValue)
                    return Convert.ChangeType((Convert.ToDouble(value) + 1), type);
                else
                    return Convert.ChangeType((Convert.ToInt64(value) + 1), type);
            }
            else if (type == typeof(string))
            {
                return Guid.NewGuid().ToString();
            }
            else if (type == typeof(Guid))
                return Guid.NewGuid();
            throw new Exception($"Unexpected type to increment: ({value.ToString()}) {type.FullName}");
        }

        public static object? GetCurrentKey<T>(Expression<Func<T, object>> expression)
        => GetCurrentKeyFromKeyName(GetExpressionKey(expression));

        public static object? GetCurrentKeyFromKeyName(string key)
        {
            incrementKeys.TryGetValue(key, out object? value);
            return value;
        }

        private static string GetExpressionKey<T>(Expression<Func<T, object>> expression)
        {
            MemberExpression member = expression.Body as MemberExpression;
            if (member == null)
            {
                // The property access might be getting converted to object to match the func
                // If so, get the operand and see if that's a member expression
                member = (expression.Body as UnaryExpression)?.Operand as MemberExpression;
            }
            if (member == null)
            {
                throw new ArgumentException("Action must be a member expression.");
            }
            return $"{typeof(T).Name}.{member.Member.Name}";
        }

        public static void IndexInitialValue<T>(Expression<Func<T, object>> expression, object initialValue)
        => IndexInitialValue(GetExpressionKey(expression), initialValue);

        public static void IndexInitialValue(string key, object initialValue)
        {
            lock (_lock)
            {
                if (initialValue == null)
                    throw new Exception("Initial value must not be null. Supply the type.");
                else if (initialValue is Type)
                    initialValue = GetInitialValue((Type)initialValue);
                initialKeys.TryAdd(key, initialValue);
                incrementKeys.TryAdd(key, initialValue);
                AddGeneratedKey(key, initialValue);
            }
        }
    }

    /// <summary>
    /// This is used to specify starting values for certain Types' Expressions.
    /// Use IncrementOrInitialize after setting initial KeyValue indices if you wish to use in loop.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class KeyValue<T> where T : class
    {
        public KeyValue(Expression<Func<T, object>> expression, object initialValue)
        {
            KeySeeder.IndexInitialValue(expression, initialValue);
        }

        public KeyValue(string index, object initialValue)
        {
            KeySeeder.IndexInitialValue(index, initialValue);
        }
    }
}