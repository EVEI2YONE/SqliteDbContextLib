using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContextLib
{
    internal class DependencyResolver
    {
        private IEnumerable<Type> EntityTypes;
        private Type[,] Matrix;
        private IDictionary<Type, int>? Dict;
        private List<PropertyMetadata> Properties;
        private List<PropertyMetadata> PkProperties;
        private List<Type> DependencyOrder;
        private List<Type> KeyfulEntities;
        private List<Type> KeylessEntities;
        public DependencyResolver(DbContext? context)
        {
            if (context == null)
                throw new ArgumentException("Must have dbContext instance supplied", nameof(context), null);
            EntityTypes = context.Model.GetEntityTypes().Select(x => x.ClrType);
            Properties = new List<PropertyMetadata>();
            PkProperties = new List<PropertyMetadata>();
            DependencyOrder = new List<Type>();
            var entities = EntityTypes.Select(x => x).OrderBy(x => x.Name);
            KeyfulEntities = new List<Type>();
            KeylessEntities = new List<Type>();
            FilterKeylessEntities(context, entities);
            Matrix = new Type[KeyfulEntities.Count(), KeyfulEntities.Count()];
            CreateTypeMatrix(KeyfulEntities);
            CreateKeyPropertiesList(context, KeyfulEntities);
            CreateDependencyOrder(CopyMatrix());
        }

        private void CreateTypeMatrix(IEnumerable<Type> entities)
        {
            int i = 0;
            Dict = new Dictionary<Type, int>();
            foreach (var type in entities)
            {
                Matrix[i, i] = type;
                Dict.Add(type, i);
                i++;
            }
            i = 0;
        }

        private void FilterKeylessEntities(DbContext context, IEnumerable<Type> entities)
        {
            foreach (var type in entities)
            {
                try
                {
                    context.Entry(Activator.CreateInstance(type));
                    KeyfulEntities.Add(type);
                }
                catch (Exception)
                {
                    KeylessEntities.Add(type);
                }
            }
        }

        private void CreateKeyPropertiesList(DbContext context, IEnumerable<Type> entities)
        {
            int i = 0;
            foreach (var type in entities)
            {
                var entry = context.Entry(Activator.CreateInstance(type));
                var fksProps = entry.Metadata.GetForeignKeys().Select(x => new PropertyMetadata()
                {
                    Property = x.Properties.First().PropertyInfo,
                    ReferencingProperty = x.PrincipalEntityType.GetProperties().First().PropertyInfo
                });
                foreach (var fk in fksProps.Select(x => x.ReferencingProperty))
                {
                    Matrix[i, Dict[fk.DeclaringType]] = fk.DeclaringType;
                }
                Properties.AddRange(fksProps);
                AddPrimaryKeys(entry.Metadata.FindPrimaryKey()?.Properties);
                i++;
            }
        }

        private void AddPrimaryKeys(IEnumerable<IProperty>? properties)
        {
            if (properties == null || !properties.Any())
                return;
            PkProperties.AddRange(properties.Select(x => new PropertyMetadata()
            {
                Property = x.PropertyInfo
            }));
        }

        private Type[,] CopyMatrix()
        {
            Type[,] copy = new Type[Matrix.GetLength(0), Matrix.GetLength(1)];
            for (int i = 0; i < copy.GetLength(0); i++)
                for (int j = 0; j < copy.GetLength(1); j++)
                    copy[i, j] = Matrix[i, j];
            return copy;
        }

        private void CreateDependencyOrder(Type[,] matrix)
        {
            bool reiterate = false;
            int count = 0;
            int reiterations = 0;
            do
            {
                bool hasDependency = false;
                for (int i = 0; i < matrix.GetLength(0); i++)
                {
                    hasDependency = MatrixTraversal(matrix, i, 0);
                    reiterate = reiterate ? true : hasDependency;
                    count++;
                }
                reiterations++;
            } while (reiterate && DependencyOrder.Count() <= matrix.Length);
            DependencyOrder = GetDistinctOrderSequence(DependencyOrder);
        }

        private List<Type> GetDistinctOrderSequence(List<Type> entities)
        {
            List<Type> distinct = new List<Type>();
            foreach (var type in entities)
                if (distinct.Any(x => x.Name == type.Name))
                    continue;
                else
                    distinct.Add(type);
            return distinct;
        }

        private bool MatrixTraversal(Type[,] matrix, int identity, int j)
        {
            bool hasDependency = false;
            for (; j < matrix.GetLength(0); j++)
            {
                if (j == identity) continue;
                var type = matrix[identity, j];
                if (type == null) continue;
                if (DependencyOrder.Contains(type))
                    matrix[identity, j] = null;
                else
                    hasDependency = true;
            }
            if (!hasDependency)
                DependencyOrder.Add(matrix[identity, identity]);
            return hasDependency;
        }

        public IEnumerable<Type> GetDependencyOrder() => DependencyOrder.AsEnumerable();
        public IEnumerable<Type> GetKeylessEntities() => KeylessEntities.AsEnumerable();
        public IEnumerable<PropertyMetadata> GetForeignKeyPropertyMetadata() => Properties.AsEnumerable();
        public IEnumerable<PropertyMetadata> GetPrimaryKeyPropertyMetadata() => PkProperties.AsEnumerable();

        public string PrintMatrix()
        {
            StringBuilder response = new StringBuilder();
            for (int i = 0; i < Matrix.GetLength(0); i++)
            {
                for (int j = 0; j < Matrix.GetLength(1); j++)
                {
                    string append = (j < Matrix.GetLength(1) - 1) ? "\t" : string.Empty;
                    string name = Matrix[i, j]?.Name ?? "_____";
                    response.Append($"{name}{append}");
                }
                response.AppendLine();
            }
            return response.ToString();
        }

        public string PrintMatrixRelationship()
        {
            StringBuilder response = new StringBuilder();
            for (int i = 0; i < Matrix.GetLength(0); i++)
            {
                response.AppendLine(Matrix[i, i].Name + ":");
                for (int j = 0; j < Matrix.GetLength(1); j++)
                {
                    if (i == j || Matrix[i, j] == null) continue;
                    response.AppendLine($"\t{Matrix[i, j].Name}");
                }
                response.AppendLine();
            }
            return response.ToString();
        }
    }


    public class PropertyMetadata
    {
        public PropertyInfo Property { get; set; }
        public PropertyInfo ReferencingProperty { get; set; }
    }
}