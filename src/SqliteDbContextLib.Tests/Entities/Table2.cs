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
    [DebuggerDisplay("T2.PK: {Col1_PK}, T1.PK: {Col2_FK}")]
    public class Table2
    {
        public Table2()
        {
            Table4 = new HashSet<Table4>();
        }

        [Key]
        public int Col1_PK { get; set; }
        public long Col2_FK { get; set; }
        public string? Col3_Value { get; set; }
        public string? Col4_Extra { get; set; }
        public int Col5_Extra { get; set; }

        public virtual Table1 Table1 { get; set; }
        public virtual ICollection<Table3> Table3 { get; set; }
        public virtual ICollection<Table4> Table4 { get; set; }
    }
}
