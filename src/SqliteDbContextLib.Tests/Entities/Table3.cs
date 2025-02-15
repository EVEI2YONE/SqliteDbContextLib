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
    [PrimaryKey(nameof(Col1_PKFK), nameof(Col2_FK))]
    [DebuggerDisplay("T1.PK: {Col1_PKFK}, T2.PK: {Col2_FK}")]
    public class Table3
    {
        public Table3()
        {
            Table4 = new HashSet<Table4>();
        }

        [Key]
        public long Col1_PKFK { get; set; }
        [Key]
        public int Col2_FK { get; set; }
        public string Col3_Value { get; set; }
        public int? Col4_Extra { get; set; }
        public string? Col5_Extra { get; set; }
        public virtual Table1 Table1 { get; set; }
        public virtual Table2 Table2 { get; set; }
        public virtual ICollection<Table4> Table4 { get; set; }
    }
}
