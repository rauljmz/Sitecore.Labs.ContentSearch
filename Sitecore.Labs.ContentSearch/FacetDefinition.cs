using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Sitecore.Labs.ContentSearch
{
    public class FacetDefinition<T>
    {
        public string DisplayName { get; set; }
        public string FacetName { get; private set; }
        public Expression<Func<T, string>> Field { get; private set; }
        public List<string> Filter { get; private set; }
        public Func<string, string> ValueToDisplayName { get; set; }
        public IEnumerable<FacetValue<T>> Values { get; set; }

        public FacetDefinition(Expression<Func<T, string>> exp)
        {
            var memberExpr = (MemberExpression)exp.Body;
            PropertyInfo property = (PropertyInfo)memberExpr.Member;
            var attr = property.GetCustomAttribute<Sitecore.ContentSearch.IndexFieldAttribute>();
            if (attr != null)
            {
                FacetName = attr.IndexFieldName;
            }
            else
            {
                FacetName = property.Name;
            }
            Field = exp;
            Filter = new List<string>();
        }

        public Expression<Func<T, bool>> CreateFilterExpression(string value)
        {
            var memberExpr = (MemberExpression)Field.Body;
            PropertyInfo property = (PropertyInfo)memberExpr.Member;

            var argExpr = Expression.Parameter(typeof(T), "p");
            var propertyExpr = Expression.Property(argExpr, property);
            var constExpr = Expression.Constant(value);

            Expression compExpr = Expression.Equal(propertyExpr, constExpr);

            return Expression.Lambda<Func<T, bool>>(compExpr, argExpr);
        }
    }
}
