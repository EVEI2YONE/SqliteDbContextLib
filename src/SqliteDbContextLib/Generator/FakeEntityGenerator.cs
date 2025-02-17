using AutoPopulate;
using Bogus;
using SqliteDbContext.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContext.Generator
{
    internal class FakeEntityGenerator : IEntityGenerator
    {
        private EntityGenerator autopopulate = new EntityGenerator();
        public FakeEntityGenerator()
        {
            autopopulate = new EntityGenerator();
            foreach(var key in typeSwitch.Keys)
                autopopulate.DefaultValues[key] = typeSwitch[key];
        }

        private static Faker f = new Faker();
        public static Dictionary<Type, Func<object>> typeSwitch = new Dictionary<Type, Func<object>> {
            { typeof(string), () => f.Random.Words(5) },
            { typeof(bool), () => f.Random.Bool() },
            { typeof(short), () => f.Random.Short(1) },
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

        public Dictionary<Type, Func<object>> DefaultValues => typeSwitch;

        public int RecursiveLimit { get; set; } = 3;
        public int CollectionLimit { get; set; } = 5;
        public int CollectionStart { get; set; } = 1;
        public EntityGenerator.RandomizationType RandomizationBehavior { get; set; } = EntityGenerator.RandomizationType.Range;

        public T CreateFake<T>() where T : class, new()
        {
            return autopopulate.CreateFake<T>();
        }

        public object? CreateFake(Type type)
        {
            return autopopulate.CreateFake(type);
        }
    }
}
