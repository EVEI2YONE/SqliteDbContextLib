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
        IEnumerable<EntityMetadata> GetEntityMetadata();
        IEnumerable<Type> GetOrderedEntityTypes();
        Expression<Func<TEntity, object>> GetPropertyLambda<TEntity>(string propertyName);
        Expression<Func<TEntity, object[]>> GetCompositeKeyLambda<TEntity>(IEnumerable<string> propertyNames);
        IQueryable<dynamic> BuildJoinQuery<TPrincipal, TDependent>(
            IQueryable<TPrincipal> principalQuery,
            IQueryable<TDependent> dependentQuery,
            string dependentForeignKey)
            where TPrincipal : class
            where TDependent : class;
    }
}
