using EntityGenerator.Generator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DbContextDriverProject
{
    public class DbContextDriver
    {
        private DbContext context;
        private ConcurrentDictionary<Type, IEnumerable<IProperty>> classKeys;
        private BogusGenerator generator;
        public DbContextDriver(DbContext context)
        {
            this.context = context;
            classKeys = new ConcurrentDictionary<Type, IEnumerable<IProperty>>();
            generator = new BogusGenerator();
            GenerateClassDictionaryKeys();
        }

        private void GenerateClassDictionaryKeys()
        {
            var properties = context.GetType().GetProperties();
            foreach (var property in properties)
            {
                var dbSetType = property.PropertyType;
                if (dbSetType.IsGenericType && dbSetType.GetGenericTypeDefinition() == typeof(DbSet<>))
                {
                    Type myType = typeof(InternalDbSet<>).MakeGenericType(dbSetType);
                    dynamic instance = Activator.CreateInstance(myType, context, dbSetType.Name);
                    var keys = FetchKeys(instance, out Type instanceType);
                    classKeys.TryAdd(instanceType, keys);
                }
            }
        }

        private IEnumerable<IProperty> FetchKeys<T>(DbSet<T> _, out Type genericType) where T : class
        {
            genericType = typeof(T).GenericTypeArguments[0];
            var entity = Activator.CreateInstance(genericType);
            var entry = context.Entry(entity);
            var keyParts = entry.Metadata.FindPrimaryKey();
            var fks = entry.Metadata.GetForeignKeys();
            var fks2 = entry.Metadata.GetForeignKeyProperties();
            var fks3 = entry.Metadata.GetReferencingForeignKeys();
            var PKs = keyParts.Properties;
            return PKs;
        }

        public void Add<T>(T obj) where T : class
        {
            context.Set<T>().Add(obj);
            context.SaveChanges();
        }

        //Create a dummy instance to spoof the binding in a generic method
        public IEnumerable<T> GetEntity<T>() where T : class
        {
            Type type = typeof(T);
            Type spoofedType = typeof(InternalDbSet<>).MakeGenericType(type);
            //instance is only used to spoof the binding
            dynamic instance = Activator.CreateInstance(spoofedType, context, spoofedType.Name);
            return SpoofedMethod(instance);
        }

        public IEnumerable<T> SpoofedMethod<T>(DbSet<T> _) where T : class
        {
            return context.Set<T>().Select(e => e);
        }
    }
}
