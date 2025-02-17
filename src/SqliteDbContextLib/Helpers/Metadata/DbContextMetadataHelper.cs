using Microsoft.EntityFrameworkCore;
using SqliteDbContext.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContext.Helpers.Metadata
{
    public static class DbContextMetadataHelper
    {
        /// <summary>
        /// Uses EF Core metadata to build a list of entity metadata objects.
        /// </summary>
        public static List<EntityMetadata> GetEntityMetadata(DbContext context) =>
            context.Model.GetEntityTypes()
                .Select(entityType => new EntityMetadata
                {
                    EntityType = entityType.ClrType,
                    PrimaryKeys = entityType.FindPrimaryKey()?.Properties
                                   .Select(p => p.Name)
                                   .ToList() ?? new List<string>(),
                    ForeignKeys = entityType.GetForeignKeys()
                                   .Select(fk => new ForeignKeyRelationship
                                   {
                                       PrincipalEntityName = fk.PrincipalEntityType.ClrType.Name,
                                       ForeignKeyProperties = fk.Properties.Select(p => p.Name).ToList()
                                   })
                                   .ToList()
                })
                .ToList();

        /// <summary>
        /// Builds a lambda expression to select a property by name.
        /// For example, for property "RegionID", returns: (TEntity e) => (object)e.RegionID.
        /// </summary>
        public static Expression<Func<TEntity, object>> BuildPropertySelector<TEntity>(string propertyName)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var property = Expression.Property(parameter, propertyName);
            var converted = Expression.Convert(property, typeof(object));
            return Expression.Lambda<Func<TEntity, object>>(converted, parameter);
        }
    }
}
