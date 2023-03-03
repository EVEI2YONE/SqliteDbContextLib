using Bogus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EntityGenerator.Generator
{
    public class BogusGenerator
    {
        public BogusGenerator()
        {
            
        }

        public T? Generate<T>(params Action<T>[] actions) where T : new()
        {
            var obj = new T();
            foreach(var action in actions)
            {
                action.Invoke(obj);
            }
            return obj;
        }  

        public object? Generate()
        {
            return null;
        }
    }
}
