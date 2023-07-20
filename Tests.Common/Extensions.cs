using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using FluentValidation;
using FluentValidation.Internal;

namespace PEXC.Case.Tests.Common;

public static class Extensions
{
    public static string GetPropertyPath<T, TProperty>(this Expression<Func<T, TProperty>> expression)
        => ValidatorOptions.Global.PropertyNameResolver(typeof(T), expression.GetMember(), expression);

    public static T WithPropertySet<T, TValue>(this T target, Expression<Func<T, TValue>> expression, TValue? value)
    {
        if (expression.Body is not MemberExpression memberExpression) throw new ArgumentException();
        if (memberExpression.Member is not PropertyInfo property) throw new ArgumentException();

        property.SetValue(GetNewTarget(target!, memberExpression.Expression!), value, null);
        return target;
    }

    private static object GetNewTarget(object currentTarget, Expression expression)
    {
        switch (expression.NodeType)
        {
            case ExpressionType.Parameter:
                return currentTarget;
            case ExpressionType.MemberAccess:
                if (expression is not MemberExpression memberExpression) throw new ArgumentException();
                if (memberExpression.Member is not PropertyInfo property) throw new ArgumentException();
                return property.GetValue(GetNewTarget(currentTarget, memberExpression.Expression!), null)!;
            default:
                throw new InvalidOperationException();
        }
    }

    public static string? GetEnumMemberAttributeValue<T>(this T enumValue) where T : struct, Enum
    {
        return typeof(T)
            .GetField(Enum.GetName(enumValue)!,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)!
            .GetCustomAttributes(typeof(EnumMemberAttribute), true)
            .Cast<EnumMemberAttribute>()
            .Select(a => a.Value)
            .SingleOrDefault();
    }
}