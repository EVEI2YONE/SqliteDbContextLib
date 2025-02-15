using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContextLibTests.Entities
{
    [PrimaryKey(nameof(Col1_PK))]
    [DebuggerDisplay("T1.PK: {Col1_PK}")]
    public class Table1
    {
        public Table1()
        {
            Table2 = new HashSet<Table2>();
            Table3 = new HashSet<Table3>();
            Table4 = new HashSet<Table4>();
        }

        [Key]
        public long Col1_PK { get; set; }
        public string? Col2 { get; set; }
        public int? Col3 { get; set; }
        public string? Col4 { get; set; }

        public virtual ICollection<Table2> Table2 { get; set; }
        public virtual ICollection<Table3> Table3 { get; set; }
        public virtual ICollection<Table4> Table4 { get; set; }
    }
}
