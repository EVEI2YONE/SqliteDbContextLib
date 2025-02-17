using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContext.Models
{
    public class EntityMetadata
    {
        public Type EntityType { get; set; }
        public List<string> PrimaryKeys { get; set; } = new List<string>();
        public List<ForeignKeyRelationship> ForeignKeys { get; set; } = new List<ForeignKeyRelationship>();
    }
}
