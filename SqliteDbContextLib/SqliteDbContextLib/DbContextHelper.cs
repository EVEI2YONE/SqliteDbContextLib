using Bogus;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDbContextLib
{
    internal class DbContextHelper
    {
        private DbContext _context { get; set; }

        public DbContextHelper(DbContext context)
        {
            _context = context;
        }

        public void Save<T>(List<T> list)
            => list.ForEach(x => Save(x));

        public void Save<T>(T item)
        {
            if (item == null) return;
            _context.Add(item);
            _context.SaveChanges();
        }

        public void Update<T>(List<T> list)
            => list.ForEach(x => Update(x));

        public void Update<T>(T item)
        {
            if (item == null) return;
            _context.Entry(item).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void Remove<T>(T item)
        {
            if (item == null) return;
            _context.Remove(item);
            _context.SaveChanges();
        }

        public void Remove<T>(List<T> list)
            => list.ForEach(x => Remove(x));

        public T? Find<T>(params object?[]? ids) where T : class => _context.Set<T>().Find(ids);

        public T ConvertItem<T>(object o) => (T)Convert.ChangeType(o, typeof(T));

        public bool TryFetchElseSave<T>(Faker<T> fakeItem, out T item, params object?[]? ids) where T : class
        {
            T? itemSaved = null;
            if (ids != null && ids.Length > 0)
                itemSaved = Find<T>(ids);
            if (itemSaved == null)
            {
                item = fakeItem.Generate(); //user input already defined prior to calling
                Save(item);
            }
            else //have to update instance based on user input
            {
                item = itemSaved;
            }
            return itemSaved != null;
        }

    }
}