using Microsoft.EntityFrameworkCore;
using SmoothBrainDevelopers.DataLayer.Test.Context;
using SmoothBrainDevelopers.DataLayer.Test.Domain;
using SqliteDbContext.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContextLib.Tests.Tests
{
    internal class TestBase
    {
        protected void RegisterDbContextDependencies(SqliteDbContext<TestDbContext> sqliteDbContext)
        {
            //sqliteDbContext.RegisterKeyAssignment<Customer>((customer, seeder, ctx) =>
            //{
            //    customer.CustomerId = (int)seeder.IncrementKeys<Customer>().First();
            //});

            //sqliteDbContext.RegisterKeyAssignment<Store>((store, seeder, ctx) =>
            //{
            //    store.StoreId = (int)seeder.IncrementKeys<Store>().First();
            //    store.RegionId = (int)seeder.GetRandomKeys<Region>().First();
            //});

            //sqliteDbContext.RegisterKeyAssignment<Product>((product, seeder, ctx) =>
            //{
            //    product.ProductId = (int) seeder.IncrementKeys<Product>().First();
            //});

            //sqliteDbContext.RegisterKeyAssignment<Purchase>((purchase, seeder, ctx) =>
            //{
            //    purchase.PurchaseId = (int)seeder.IncrementKeys<Purchase>().First();
            //    purchase.CustomerId = (int)seeder.GetRandomKeys<Customer>().First();
            //    purchase.ProductId = (int)seeder.GetRandomKeys<Product>().First();
            //    purchase.StoreId = (int)seeder.GetRandomKeys<Store>().First();
            //});

            //sqliteDbContext.RegisterKeyAssignment<Region>((region, seeder, ctx) =>
            //{
            //    region.RegionId = (int)seeder.IncrementKeys<Region>().First();
            //});

            //sqliteDbContext.RegisterKeyAssignment<Sale>((sale, seeder, ctx) =>
            //{
            //    sale.SaleId = (int)seeder.IncrementKeys<Sale>().First();
            //    sale.StoreId = (int)seeder.GetRandomKeys<Store>().First();
            //});

            
        }
    }
}
