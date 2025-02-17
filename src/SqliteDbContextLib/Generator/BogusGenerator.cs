using AutoPopulate;
using Bogus;
using Microsoft.EntityFrameworkCore;
using SqliteDbContext.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContext.Generator
{
    /// <summary>
    /// Uses Bogus to generate fake entities, then clears key values and defers to KeySeeder for key assignment.
    /// </summary>
    public class BogusGenerator
    {
        private readonly IDependencyResolver _dependencyResolver;
        private readonly IKeySeeder _keySeeder;
        private readonly IEntityGenerator _entityGenerator;

        public BogusGenerator(IDependencyResolver dependencyResolver, IKeySeeder keySeeder, IEntityGenerator entityGenerator)
        {
            _dependencyResolver = dependencyResolver;
            _keySeeder = keySeeder;
            _entityGenerator = entityGenerator;
        }

        public T GenerateFake<T>() where T : class, new()
        {
            var faker = new Faker<T>()
                .CustomInstantiator(f =>
                {
                    var item = (T) _entityGenerator.CreateFake(typeof(T));
                    return item;

                });
            // Generate fake data.
            var entity = faker.Generate();
            return entity;
        }

        /// <summary>
        /// Clears navigation properties (virtual one-to-one, one-to-many, etc.) from the generated entity.
        /// Reference navigation properties are set to null and collection navigation properties are set to an empty collection.
        /// </summary>
        public T RemoveNavigationProperties<T>(T entity) where T : class
        {
            var type = typeof(T);
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // Only consider writable properties.
                if (!prop.CanWrite)
                    continue;

                // Check if the property is virtual and not final (i.e. can be overridden).
                var getter = prop.GetGetMethod();
                if (getter == null || !getter.IsVirtual || getter.IsFinal)
                    continue;

                // Skip strings.
                if (prop.PropertyType == typeof(string))
                    continue;

                // If property is a collection (and implements IEnumerable), clear it.
                if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
                {
                    // For interfaces (like ICollection<T>), create a new List<T>.
                    if (prop.PropertyType.IsInterface)
                    {
                        var elementType = prop.PropertyType.GetGenericArguments().FirstOrDefault();
                        if (elementType != null)
                        {
                            var listType = typeof(List<>).MakeGenericType(elementType);
                            var emptyList = Activator.CreateInstance(listType);
                            prop.SetValue(entity, emptyList);
                        }
                        else
                        {
                            prop.SetValue(entity, null);
                        }
                    }
                    else
                    {
                        // For concrete collection types with a parameterless constructor.
                        var ctor = prop.PropertyType.GetConstructor(Type.EmptyTypes);
                        if (ctor != null)
                        {
                            var instance = ctor.Invoke(null);
                            prop.SetValue(entity, instance);
                        }
                        else
                        {
                            prop.SetValue(entity, null);
                        }
                    }
                }
                else
                {
                    // For non-collection reference types, set to null.
                    prop.SetValue(entity, null);
                }
            }
            return entity;
        }

        public T ClearNewNavigationProperties<T>(T entity, DbContext context) where T : class
        {
            // Iterate over public instance properties.
            var type = typeof(T);
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite)
                    continue;
                if (prop.PropertyType == typeof(string))
                    continue;
                // Skip collections.
                if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string))
                    continue;

                // Only consider virtual (overridable) properties.
                var getter = prop.GetGetMethod();
                if (getter == null || !getter.IsVirtual || getter.IsFinal)
                    continue;

                var navigationValue = prop.GetValue(entity);
                if (navigationValue == null)
                    continue;

                // Check if the navigation value is being tracked.
                var entry = context.Entry(navigationValue);
                // If the referenced entity is new (state Added) then clear the navigation property.
                if (entry.State == EntityState.Added)
                {
                    prop.SetValue(entity, null);
                }
                else
                {
                    // Alternatively, if the referenced entity's key is still at its default value, clear it.
                    var foreignKey = context.Model.FindEntityType(navigationValue.GetType()).FindPrimaryKey();
                    if (foreignKey != null)
                    {
                        var keyProp = foreignKey.Properties.First().PropertyInfo;
                        var keyValue = keyProp.GetValue(navigationValue);
                        if (Equals(keyValue, GetDefault(keyProp.PropertyType)))
                        {
                            prop.SetValue(entity, null);
                        }
                    }
                }
            }
            return entity;
        }

        /// <summary>
        /// Helper method to return the default value for a given type.
        /// </summary>
        private static object GetDefault(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}