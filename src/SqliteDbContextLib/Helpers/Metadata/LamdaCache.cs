using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContext.Helpers.Metadata
{
    /// <summary>
    /// Caches compiled lambda expressions keyed by "FullTypeName.PropertyName".
    /// </summary>
    public static class LambdaCache
    {
        private static readonly ConcurrentDictionary<string, LambdaExpression> Cache = new ConcurrentDictionary<string, LambdaExpression>();

        public static Expression<Func<TEntity, object>> GetOrAdd<TEntity>(string propertyName)
        {
            var key = $"{typeof(TEntity).FullName}.{propertyName}";
            if (Cache.TryGetValue(key, out var cached))
            {
                return (Expression<Func<TEntity, object>>)cached;
            }
            var lambda = DbContextMetadataHelper.BuildPropertySelector<TEntity>(propertyName);
            Cache[key] = lambda;
            return lambda;
        }
    }
}
