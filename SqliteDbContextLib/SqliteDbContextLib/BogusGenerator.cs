using AutoPopulate_Generator;
using Bogus;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContextLib
{
    public class BogusGenerator<T> where T : class
    {
        private DbContext dbcontext;
        private IEnumerable<PropertyMetadata> foreignKeyPropertyMetadata;
        private IEnumerable<PropertyMetadata> primaryKeyPropertyMetadata;
        private AutoPopulate autopopulate = new AutoPopulate();
        public BogusGenerator(DbContext context, IEnumerable<PropertyMetadata> primaryKeyProperties, IEnumerable<PropertyMetadata> foreignKeyProperties)
        {
            primaryKeyPropertyMetadata = primaryKeyProperties;
            foreignKeyPropertyMetadata = foreignKeyProperties;
            AutoPopulate.DefaultValues = typeSwitch;
            dbcontext = context;
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

        public E Generate<E>() where E : class, new()
        {
            var fakeList = new Faker<E>() //customly define new item to be generated
                  .CustomInstantiator(f =>
                  {
                      var item = (E)autopopulate.CreateFake(typeof(E));
                      return item;
                  });
            return fakeList.Generate();
        }

        public void ClearKeys(object? item)
        {
            if (item == null)
                return;
            var type = item.GetType();
            var properties = primaryKeyPropertyMetadata.Where(x => x.Property.ReflectedType == type)
                      .Union(foreignKeyPropertyMetadata.Where(x => x.Property.ReflectedType == type));//.Select(x => x.Property);//FetchProperties<E>();
            foreach (var propertyMetadata in properties)
            {
                var reference = propertyMetadata.ReferencingProperty;
                var referencedObjs = GetReferencedObjects(item, reference);
                if (referencedObjs != null)
                    foreach (var referencedObj in referencedObjs)
                        ClearKeys(referencedObj);
                var property = propertyMetadata.Property;
                var propertyType = ExtractNullableType(property.PropertyType);
                if (propertyType.IsPrimitive)
                {
                    property.SetValue(item, Convert.ChangeType(-1, propertyType));
                }
                else if (propertyType == typeof(string))
                    property.SetValue(item, null);
                else if (propertyType == typeof(Guid))
                    property.SetValue(item, Guid.Empty);
            }
        }

        private IEnumerable<object?>? GetReferencedObjects(object item, PropertyInfo reference)
          => (item == null) ? null : item.GetType().GetProperties().Where(x => (reference == null) ? false : reference.DeclaringType == x.PropertyType).Select(x => x.GetValue(item));

        public void ApplyInitializingAction<E>(E entity, Action<E> initializeAction) where E : class
        {
            if (initializeAction == null)
                return;
            initializeAction(entity);
        }

        /* 
         * A generated entity has new references whose values are always randomly generated. 
         * How to choose between generating a new PK for a nested referenced value, or 
         * choose a pre-existing referenced item already generated
         * e.g. Will both reference C2, or will a seperate entity (C1) be created?
         * ItemA-ItemC1 (newly generating)
         *      \
         *       ItemC2
         *      /
         * ItemB (already generated)
         * Solution: Use RNG to determine which path to take
         */

        public void PopulateEntityKeys<E>(E? entity) where E : class
        {
            List<object> reuseableEntities = new List<object>();
            RecursivelyPopulateEntityKeys(entity, reuseableEntities);
        }
        private void RecursivelyPopulateEntityKeys<E>(E? entity, List<object> reuseableEntities) where E : class
        {
            if (entity == null) //for recursive FK reference calls
                return;
            var entityType = entity.GetType();
            var entityFKs = foreignKeyPropertyMetadata.Where(x => x.Property.ReflectedType == entityType);
            //recursively start with FKs then increment PKs
            foreach (var fk in entityFKs) //get random 1-N existing reference value
            {
                var foreignReferences = GetReferencedObjects(entity, fk.ReferencingProperty)?.Where(x => x != null);
                if (foreignReferences == null || !foreignReferences.Any())
                    continue;
                foreach (var foreignReference in foreignReferences)
                {
                    RecursivelyPopulateEntityKeys(foreignReference, reuseableEntities);
                    //set Entity's FK to ReferencedEntity's PK
                    var value = fk.ReferencingProperty.GetValue(foreignReference);
                    if (value == null)
                        continue;
                    fk.Property.SetValue(entity, value);
                }
                //Update key constraints here
            }

            var entityPKs = primaryKeyPropertyMetadata.Where(x => x.Property.ReflectedType == entityType);
            var pks = entityPKs.Select(x => x.Property.GetValue(entity)).ToArray();
            var savedItem = Find(entityType, pks);
            if (savedItem != null)
                return;
            int attempts;
            bool fetchExistingKey;
            foreach (var pk in entityPKs) //increment primary keys
            {
                if (IsOverridedValue(pk))
                    continue;
                var pkValue = pk.Property.GetValue(entity);
                attempts = 0;
                //randomly reference PKs or if PKs are also FKs
                fetchExistingKey = false;// KeySeeder.Random.Next(0, 2) == 1 || entityFKs.Any(x => entityPKs.Any(y => y.Property.Name == x.Property.Name));
                //may have to introduce some expression or pull metadata to resolve constraints
                bool success = false;
                var keyName = GetKeyName(pk.Property);
                do
                {
                    try
                    {
                        var keyValue = new object();
                        if (fetchExistingKey && false)//KeySeeder.TryGetRandomKeyFromName(keyName, out object? keyValue))
                        {
                            pk.Property.SetValue(entity, keyValue);
                            success = true;
                        }
                        else
                        {
                            pk.Property.SetValue(entity, 1);//)Convert.ChangeType(KeySeeder.IncrementOrInitializeFromName<E>(keyName, pk.Property.PropertyType), pk.Property.PropertyType));
                            success = true;
                        }
                        break;
                    }
                    catch (Exception ex)
                    {

                    }
                } while (attempts++ < 5);
                if (!success)
                    pk.Property.SetValue(entity, 1);// Convert.ChangeType(KeySeeder.IncrementOrInitializeFromName<E>(keyName, pk.Property.PropertyType), pk.Property.PropertyType));
            }
            dbcontext.Add(entity);
            dbcontext.SaveChanges();
            reuseableEntities.Add(entity);
        }

        private Dictionary<string, dynamic> spoofedInstances = new Dictionary<string, dynamic>();
        private dynamic Find(Type type, params object[]? pks)
        {
            if (!spoofedInstances.TryGetValue(type.Name, out dynamic? spoofedInstance))
            {
                spoofedInstances.TryAdd(type.Name, Activator.CreateInstance(type));
                spoofedInstance = spoofedInstances[type.Name];
            }
            return SpoofedFind(spoofedInstance, pks);
        }

        private E? SpoofedFind<E>(E _, params object[]? pks) where E : class
          => dbcontext.Find<E>(pks);


        private bool IsOverridedValue(object value)
          => (value == null) ? false : (value.ToString() == "-1" || value.ToString() == Guid.Empty.ToString());

        private string GetKeyName(PropertyInfo property)
          => $"{property.DeclaringType.Name}.{property.Name}";

        private Type ExtractNullableType(Type propType)
        {
            Type nullableType = Nullable.GetUnderlyingType(propType);
            return nullableType ?? propType;
        }

        private static Dictionary<Type, IEnumerable<PropertyInfo>> DictionaryProperties = new Dictionary<Type, IEnumerable<PropertyInfo>>();
        private static object DictionaryLock = new object();
        private IEnumerable<PropertyInfo> FetchProperties<E>() where E : class
        {
            lock (DictionaryLock)
            {
                var type = typeof(E);
                if (!DictionaryProperties.ContainsKey(type))
                    DictionaryProperties.Add(type, FilterProperties(type.GetProperties()));
                return DictionaryProperties[type];
            }
            return null;
        }

        private IEnumerable<PropertyInfo> FilterProperties(IEnumerable<PropertyInfo> properties)
        {
            return properties.Where(x =>
            {
                var type = x.PropertyType;
                return (type == typeof(string) || type.IsPrimitive || type == typeof(Guid) || type == typeof(DateTime));
            });
        }

        private void printkeys(IEnumerable<PropertyMetadata> metadata)
        {
            int i = 0;
            foreach (var k in metadata)
                Console.WriteLine($"PK[{i++}] : {k.Property.DeclaringType.Name}.{k.Property.Name}");
        }
    }
}