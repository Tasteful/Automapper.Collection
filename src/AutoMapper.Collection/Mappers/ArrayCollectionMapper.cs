using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoMapper.Internal;
using AutoMapper.Mappers;

namespace AutoMapper.Mappers
{
    public class ArrayCollectionMapper : IObjectMapper
    {
        public object Map(ResolutionContext context)
        {
            Type genericType = typeof(EnumerableMapper<,>);

            var collectionType = context.DestinationType;
            var elementType = TypeHelper.GetElementType(context.DestinationType);

            var enumerableMapper = genericType.MakeGenericType(collectionType, elementType);

            var objectMapper = (IObjectMapper)Activator.CreateInstance(enumerableMapper);

            return objectMapper.Map(context);
        }

        public bool IsMatch(TypePair context)
        {
            var isMatch = context.SourceType.IsEnumerableType() && context.DestinationType.IsCollectionType();

            return isMatch;
        }

        #region Nested type: EnumerableMapper

        private class EnumerableMapper<TCollection, TElement> : EnumerableMapperBase<TCollection>
            where TCollection : ICollection<TElement>
        {
            public override bool IsMatch(TypePair context)
            {
                throw new NotImplementedException();
            }

            protected override void SetElementValue(TCollection destination, object mappedValue, int index)
            {
                destination.Add((TElement)mappedValue);
            }

            protected override void ClearEnumerable(TCollection enumerable)
            {
                enumerable.Clear();
            }

            protected override object GetOrCreateDestinationObject(ResolutionContext context, Type destElementType, int sourceLength)
            {
                // If the source is an array, assume we can add to it...
                if (context.DestinationValue is Array)
                    return CreateDestinationObjectBase(destElementType, sourceLength);

                return base.GetOrCreateDestinationObject(context, destElementType, sourceLength);
            }

            protected override TCollection CreateDestinationObjectBase(Type destElementType, int sourceLength)
            {
                Object collection;

                if (typeof(TCollection).GetTypeInfo().IsInterface)
                {
                    collection = new List<TElement>();
                }
                else
                {
                    collection = ObjectCreator.CreateDefaultValue(typeof(TCollection));
                }

                return (TCollection)collection;
            }
        }

        #endregion
    }

    public class ArrayCollectionMapperProfile : Profile
    {
        private readonly object _mapperLock = new object();
        protected override void Configure()
        {
            InsertBefore<ReadOnlyCollectionMapper>(new ArrayCollectionMapper());
        }

        private void InsertBefore<TObjectMapper>(IObjectMapper mapper)
            where TObjectMapper : IObjectMapper
        {
            lock (_mapperLock)
            {
                var targetMapper = MapperRegistry.Mappers.FirstOrDefault(om => om is TObjectMapper);
                var index = targetMapper == null ? 0 : MapperRegistry.Mappers.IndexOf(targetMapper);
                MapperRegistry.Mappers.Insert(index, mapper);
            }
        }
    }
}