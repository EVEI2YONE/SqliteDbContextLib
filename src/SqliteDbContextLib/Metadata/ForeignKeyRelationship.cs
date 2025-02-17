using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContext.Metadata
{
    /// <summary>
    /// Represents a foreign key relationship.
    /// </summary>
    public class ForeignKeyRelationship
    {
        public string PrincipalEntityName { get; set; }
        public List<string> ForeignKeyProperties { get; set; } = new List<string>();
    }
}
