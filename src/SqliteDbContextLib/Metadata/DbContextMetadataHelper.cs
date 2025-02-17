using Microsoft.EntityFrameworkCore;
using SqliteDbContext.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContext.Metadata
{
    public static class DbContextMetadataHelper
    {
        public static List<EntityMetadata> GetEntityMetadata(DbContext context) =>
            context.Model.GetEntityTypes()
                .Select(entityType => new EntityMetadata
                {
                    EntityType = entityType.ClrType,
                    PrimaryKeys = entityType.FindPrimaryKey()?.Properties.Select(p => p.Name).ToList() ?? new List<string>(),
                    ForeignKeys = entityType.GetForeignKeys()
                        .Select(fk => new ForeignKeyRelationship
                        {
                            PrincipalEntityName = fk.PrincipalEntityType.ClrType.Name,
                            ForeignKeyProperties = fk.Properties.Select(p => p.Name).ToList()
                        }).ToList()
                }).ToList();

        /// <summary>
        /// Builds a lambda to select a single property.
        /// e.g. (TEntity e) => (object)e.PropertyName.
        /// </summary>
        public static Expression<Func<TEntity, object>> BuildPropertySelector<TEntity>(string propertyName)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var property = Expression.Property(parameter, propertyName);
            var converted = Expression.Convert(property, typeof(object));
            return Expression.Lambda<Func<TEntity, object>>(converted, parameter);
        }

        /// <summary>
        /// Builds a lambda to select a composite key as an object array.
        /// e.g. (TEntity e) => new object[] { (object)e.Key1, (object)e.Key2 }.
        /// </summary>
        public static Expression<Func<TEntity, object[]>> BuildCompositeKeySelector<TEntity>(IEnumerable<string> propertyNames)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var props = propertyNames.Select(propName =>
            {
                var property = Expression.Property(parameter, propName);
                return Expression.Convert(property, typeof(object));
            }).ToArray();
            var arrayInit = Expression.NewArrayInit(typeof(object), props);
            return Expression.Lambda<Func<TEntity, object[]>>(arrayInit, parameter);
        }
    }
}
