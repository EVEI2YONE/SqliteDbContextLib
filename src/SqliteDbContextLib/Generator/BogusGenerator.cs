using AutoPopulate;
using Bogus;
using Microsoft.EntityFrameworkCore;
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
    /// <summary>
    /// Uses Bogus to generate fake entities, then clears key values and defers to KeySeeder for key assignment.
    /// </summary>
    public class BogusGenerator
    {
        private EntityGenerator autopopulate = new EntityGenerator();
        private static Faker f = new Faker();
        public static Dictionary<Type, Delegate> typeSwitch = new Dictionary<Type, Delegate> {
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

        private readonly IDependencyResolver _dependencyResolver;
        private readonly KeySeeder _keySeeder;

        public BogusGenerator(IDependencyResolver dependencyResolver, KeySeeder keySeeder)
        {
            _dependencyResolver = dependencyResolver;
            _keySeeder = keySeeder;
        }

        public T GenerateFake<T>() where T : class, new()
        {
            var faker = new Faker<T>()
                .CustomInstantiator(f =>
                {
                    var item = (T) autopopulate.CreateFake(typeof(T));
                    return item;

                });
            // Generate fake data.
            var entity = faker.Generate();
            return entity;
        }
    }
}