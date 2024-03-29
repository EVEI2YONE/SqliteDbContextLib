﻿using AutoPopulate_Generator;
using Bogus;
using Microsoft.EntityFrameworkCore;
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
    public class BogusGenerator
    {
        private DbContext dbcontext;
        private AutoPopulate autopopulate = new AutoPopulate();
        private IKeySeeder keySeeder = new KeySeeder();
        public BogusGenerator(DbContext? context)
        {
            if (context == null)
                throw new ArgumentException("Must have value supplied", nameof(context), null);
            AutoPopulate.DefaultValues = typeSwitch;
            dbcontext = context;
            keySeeder.ClearAllKeys();
        }

        private static Faker f = new Faker();
        public static Dictionary<Type, Delegate> typeSwitch = new Dictionary<Type, Delegate> {
            { typeof(string), () => f.Random.Words(5) },
            { typeof(bool), () => f.Random.Bool() },
            { typeof(Int16), () => f.Random.Short(1) },
            { typeof(int), () => f.Random.Int(1) },
            { typeof(long), () => f.Random.Long(1) },
            { typeof(decimal), () => f.Random.Decimal(1) },
            { typeof(double), () => f.Random.Double(1) },
            { typeof(float), () => f.Random.Float(1) },
            { typeof(char), () => f.Random.Char() },
            { typeof(byte), () => f.Random.Byte() },
            { typeof(DateTime), () => f.Date.Recent(365) },
            { typeof(Guid), () => f.Random.Guid() },
        };

        private IEnumerable<PropertyInfo> GetKeyProperties(object? obj)
            => obj == null ? new List<PropertyInfo>() : obj.GetType().GetProperties().Where(x => x.GetCustomAttribute<KeyAttribute>() != null);

        public E Generate<E>() where E : class
        {
            var fakeList = new Faker<E>() //customly define new item to be generated
                .CustomInstantiator(f =>
                {
                var item = (E)autopopulate.CreateFake(typeof(E));
                return item;
                });
            return fakeList.Generate();
        }

        public void RemoveGeneratedReferences(object? item)
        {
            if (item == null)
                return;
            foreach(var property in item.GetType().GetProperties())
            {
                var propertyType = property.PropertyType;
                var type = (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>)) ? propertyType.UnderlyingSystemType : propertyType;
                if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime))
                    continue;
                if (type.IsGenericType)
                {
                    var genericType = type.GetGenericTypeDefinition();
                    if (genericType == typeof(ICollection<>) || genericType == typeof(IDictionary<,>) || genericType == typeof(HashSet<>) || genericType == typeof(IList<>))
                        continue;
                }
                property.SetValue(item, null);
            }
        }

        public void ClearKeys(object? item)
        {
            if (item == null)
                return;
            var type = item.GetType();
            var properties = type.GetProperties();
            var keyProperties = GetKeyProperties(item);

            //clear keys
            if (keyProperties.Any())
                keyProperties.ToList().ForEach(x =>
                {
                    if (x.PropertyType == typeof(string))
                    {
                        x.SetValue(item, (string?)null);
                    }
                    else
                    {
                        x.SetValue(item, Convert.ChangeType(-1, x.PropertyType));
                    }
                });
        }

        public void ApplyInitializingAction<E>(E entity, Action<E>? initializeAction) where E : class
        {
            if (initializeAction == null)
                return;
            initializeAction(entity);
        }

        public void ApplyDependencyAction<E>(E entity, Action<E, IKeySeeder> dependencyAction) where E : class
        {
            dependencyAction(entity, keySeeder);
        }
    }
}