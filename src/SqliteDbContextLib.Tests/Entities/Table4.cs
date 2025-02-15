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
    [PrimaryKey(nameof(Col1_T1PKFK), nameof(Col2_T2PKFK),
        nameof(Col3_T3PKFK_PKFK), nameof(Col4_T3PKFK_FK))]
    [DebuggerDisplay("T1.PK: {Col1_T1PKFK}, T2.PK: {Col2_T2PKFK}, T3.PK1: {Col3_T3PKFK_PKFK}, T3.PK2: {Col4_T3PKFK_FK}")]
    public class Table4
    {
        [Key]
        public long Col1_T1PKFK { get; set; }
        [Key]
        public int Col2_T2PKFK { get; set; }
        [Key]
        public long Col3_T3PKFK_PKFK { get; set; }
        [Key]
        public int Col4_T3PKFK_FK { get; set; }
        public string? Col5_Value { get; set; }
        public int? Col6_Extra { get; set; }
        public string? Col7_Extra { get; set; }

        public virtual Table1 Table1 { get; set; }
        public virtual Table2 Table2 { get; set; }
        public virtual Table3 Table3 { get; set; }
    }
}
