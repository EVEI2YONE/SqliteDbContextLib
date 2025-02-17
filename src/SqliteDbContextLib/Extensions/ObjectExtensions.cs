using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContext.Extensions
{
    internal static class ObjectExtensions
    {
        public static object[] GetKeys(this object entity)
        {
            var properties = entity.GetType().GetProperties().Where(x => x.GetCustomAttribute<KeyAttribute>() != null);
            return properties.Select(x => x.GetValue(entity)).ToArray();
        }
    }
}
