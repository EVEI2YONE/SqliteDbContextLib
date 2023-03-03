using Bogus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EntityGenerator.Generator
{
    public class DefaultGenerationScheme
    {
        //types, instances, properties => resolving values, logic dependencies => generic, custom implementations
        public DefaultGenerationScheme(ICollection<Type> types)
        {
            
        }

        public void ProvideTypeScheme(Type type)
        {

        }

        //public void ProviderPropertyClassSchema<T, K>(Expression<Func<T, K>> propertyExpression) where T : class where K : class
        //{

        //}

        public void ProvidePropertyPrimitiveSchema<T>(Expression<Func<T, object>> propertyExpression) where T : class
        {
            var body = propertyExpression.Body;
            var memberName = (body as MemberExpression ?? ((UnaryExpression)body).Operand as MemberExpression).Member.Name;
            var classType = typeof(T);
            var memberProperty = classType.GetProperty(memberName);

            //switch (Type.GetTypeCode(memberProperty))
            //{
            //    case TypeCode.Boolean:
            //        break;

            //}
        }

        public object GenerateObject()
        {
            return null;
        }
    }
}
