using Microsoft.EntityFrameworkCore;
using SqliteDbContext.Context;
using SqliteDbContextLibTests.Context;
using SqliteDbContextLibTests.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContextLib.Tests.Tests
{
    internal class TestBase
    {
        protected void RegisterDbContextDependencies(SqliteDbContext<EntityProjectContext> sqliteDbContext)
        {
            sqliteDbContext.RegisterKeyAssignment<Table1>((table, seeder, ctx) =>
            {
                table.Col1_PK = seeder.IncrementKeys<Table1>().First();
            });

            sqliteDbContext.RegisterKeyAssignment<Table2>((table, seeder, ctx) =>
            {
                table.Col1_PK = (int)seeder.IncrementKeys<Table2>().First();
                table.Col2_FK = seeder.GetRandomKeys<Table1>().First();
            });

            sqliteDbContext.RegisterKeyAssignment<Table3>((table, seeder, ctx) =>
            {
                var query = ctx.Table2.Select(x => new object[] { x.Table1.Col1_PK, x.Col1_PK });
                var keys = seeder.GetUniqueRandomKeys<Table3>(ctx, query);
                table.Col1_PKFK = (long)keys.First();
                table.Col2_FK = (int)keys.Last();
            });

            sqliteDbContext.RegisterKeyAssignment<Table4>((table, seeder, ctx) =>
            {
                var query = ctx.Table3.Select(x => new object[] { x.Table1.Col1_PK, x.Table2.Col1_PK, x.Col1_PKFK, x.Col2_FK });

                var keys = seeder.GetUniqueRandomKeys<Table4>(ctx, query);
                table.Col1_T1PKFK = (long)keys.GetValue(0);
                table.Col2_T2PKFK = (int)keys.GetValue(1);
                table.Col3_T3PKFK_PKFK = (long)keys.GetValue(2);
                table.Col4_T3PKFK_FK = (int)keys.GetValue(3);
            });
        }
    }
}
