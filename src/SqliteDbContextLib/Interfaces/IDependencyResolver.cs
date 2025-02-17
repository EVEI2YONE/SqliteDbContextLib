using SqliteDbContext.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContext.Interfaces
{
    /// <summary>
    /// Contract for resolving dependency and key metadata from a DbContext.
    /// </summary>
    public interface IDependencyResolver
    {
        /// <summary>
        /// Retrieves metadata for all entities in the model.
        /// </summary>
        IEnumerable<EntityMetadata> GetEntityMetadata();

        /// <summary>
        /// Returns entity types ordered by dependency (least-dependent first).
        /// </summary>
        IEnumerable<Type> GetOrderedEntityTypes();

        /// <summary>
        /// Retrieves a cached lambda expression for selecting a property by name.
        /// </summary>
        Expression<Func<TEntity, object>> GetPropertyLambda<TEntity>(string propertyName);

        /// <summary>
        /// Builds a dynamic join query between two entities based on the key relationship.
        /// </summary>
        IQueryable<dynamic> BuildJoinQuery<TPrincipal, TDependent>(
            IQueryable<TPrincipal> principalQuery,
            IQueryable<TDependent> dependentQuery,
            string dependentForeignKey)
            where TPrincipal : class
            where TDependent : class;
    }
}
