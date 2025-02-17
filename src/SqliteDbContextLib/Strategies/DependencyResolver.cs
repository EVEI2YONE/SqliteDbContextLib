using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SqliteDbContext.Interfaces;
using SqliteDbContext.Metadata;
using SqliteDbContext.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContext.Strategies
{
    /// <summary>
    /// Resolves dependency and key metadata from a DbContext and provides helper methods for dynamic query construction.
    /// </summary>
    public class DependencyResolver : IDependencyResolver
    {
        private readonly DbContext _context;
        private readonly List<EntityMetadata> _entityMetadata;

        public DependencyResolver(DbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _entityMetadata = DbContextMetadataHelper.GetEntityMetadata(_context);
        }

        public IEnumerable<EntityMetadata> GetEntityMetadata() => _entityMetadata;

        public IEnumerable<Type> GetOrderedEntityTypes() =>
            _entityMetadata
                .OrderBy(meta => meta.ForeignKeys.Sum(fk => fk.ForeignKeyProperties.Count))
                .Select(meta => meta.EntityType)
                .ToList();

        public Expression<Func<TEntity, object>> GetPropertyLambda<TEntity>(string propertyName) =>
            LambdaCache.GetOrAdd<TEntity>(propertyName);

        public Expression<Func<TEntity, object[]>> GetCompositeKeyLambda<TEntity>(IEnumerable<string> propertyNames) =>
            LambdaCache.GetOrAddComposite<TEntity>(propertyNames);

        public IQueryable<dynamic> BuildJoinQuery<TPrincipal, TDependent>(
            IQueryable<TPrincipal> principalQuery,
            IQueryable<TDependent> dependentQuery,
            string dependentForeignKey)
            where TPrincipal : class
            where TDependent : class
        {
            var principalMeta = _entityMetadata.FirstOrDefault(m => m.EntityType == typeof(TPrincipal));
            if (principalMeta == null || !principalMeta.PrimaryKeys.Any())
                throw new InvalidOperationException("Principal entity does not have a primary key defined.");

            // For simplicity, use the first primary key.
            var principalKey = principalMeta.PrimaryKeys.First();
            var principalLambda = GetPropertyLambda<TPrincipal>(principalKey);
            var dependentLambda = GetPropertyLambda<TDependent>(dependentForeignKey);

            return principalQuery.Join(dependentQuery, principalLambda, dependentLambda,
                (principal, dependent) => new { Principal = principal, Dependent = dependent });
        }
    }
}