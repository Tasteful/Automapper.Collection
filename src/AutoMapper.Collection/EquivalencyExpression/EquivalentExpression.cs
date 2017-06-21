using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoMapper.Collection;

namespace AutoMapper.EquivalencyExpression
{
    internal class EquivalentExpression : IEquivalentExpression
    {
        internal static IEquivalentExpression BadValue { get; private set; }

        static EquivalentExpression()
        {
            BadValue = new EquivalentExpression();
        }
    }

    internal class EquivalentExpression<TSource, TDestination> : IEquivalentExpression<TSource, TDestination>
        where TSource : class
        where TDestination : class
    {
        private readonly Expression<Func<TSource, TDestination, bool>> _EquivalentExpression;
        private readonly Func<TSource, TDestination, bool> _EquivalentFunc;

        public EquivalentExpression(Expression<Func<TSource, TDestination, bool>> EquivalentExpression)
        {
            _EquivalentExpression = EquivalentExpression;
            _EquivalentFunc = _EquivalentExpression.Compile();
        }

        public bool IsEquivalent(TSource source, TDestination destination)
        {
            return _EquivalentFunc(source, destination);
        }

        public Expression<Func<TDestination, bool>> ToSingleSourceExpression(TSource source)
        {
            if (source == null)
                throw new Exception("Invalid somehow");

            var expression = new ParametersToConstantVisitor<TSource>(source).Visit(_EquivalentExpression) as LambdaExpression;
            return Expression.Lambda<Func<TDestination, bool>>(expression.Body, _EquivalentExpression.Parameters[1]);
        }

        public TDestinationItem Map<TSourceItem, TDestinationItem>(TSourceItem source, TDestinationItem destination, ResolutionContext context)
            where TSourceItem : IEnumerable<TSource>
            where TDestinationItem : class, ICollection<TDestination>
        {
            if (source == null || destination == null)
                return destination;

            var destList = destination.ToList();
            var compareSourceToDestination = source.ToDictionary(s => s, s =>
            {
                var match = destList.FirstOrDefault(d => IsEquivalent(s, d));
                destList.Remove(match);
                return match;
            });

            foreach (var removedItem in destination.Except(compareSourceToDestination.Values).ToList())
                destination.Remove(removedItem);

            foreach (var keypair in compareSourceToDestination)
            {
                if (keypair.Value == null)
                    destination.Add((TDestination)context.Mapper.Map(keypair.Key, null, typeof(TSource), typeof(TDestination), context));
                else
                    context.Mapper.Map(keypair.Key, keypair.Value, context);
            }

            return destination;
        }
    }
    internal class ParametersToConstantVisitor<T> : ExpressionVisitor
    {
        private readonly T _value;

        public ParametersToConstantVisitor(T value)
        {
            _value = value;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member is PropertyInfo && node.Member.DeclaringType.GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo()))
            {
                var memberExpression = Expression.Constant(node.Member.GetMemberValue(_value));
                return memberExpression;
            }

            return base.VisitMember(node);
        }
    }
}