﻿using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
#if NETSTANDARD
using ZeroFormatter.Extensions.Internal.FSharp;
#else
using Microsoft.FSharp.Reflection;
#endif
using System;
using System.Reflection;
using ZeroFormatter.Extensions;

namespace ZeroFormatter.Formatters
{
    public class FSharpResolver<TTypeResolver> : ITypeResolver
        where TTypeResolver : ITypeResolver, new()
    {
        private readonly TTypeResolver resolver;

        public FSharpResolver()
        {
            this.resolver = new TTypeResolver();
        }

        public bool IsUseBuiltinSerializer
        {
            get
            {
                return false;
            }
        }

        public void RegisterDynamicUnion(Type unionType, DynamicUnionResolver resolver)
        {
            this.resolver.RegisterDynamicUnion(unionType, resolver);
        }

        public object ResolveFormatter(Type type)
        {
            var resolverType = typeof(FSharpResolver<TTypeResolver>);

            if (type == typeof(Unit))
            {
                return new UnitFormatter<FSharpResolver<TTypeResolver>>();
            }

            var isGenericType = type.GetTypeInfo().IsGenericType;

            if (isGenericType && type.GetGenericTypeDefinition() == typeof(FSharpOption<>))
            {
                var vt = type.GetTypeInfo().GetGenericArguments()[0];
                if (FSharpType.IsRecord(vt, null))
                {
                    var formatter = typeof(FSharpOptionRecordFormatter<,>).MakeGenericType(resolverType, vt);
                    return Activator.CreateInstance(formatter);
                }
                else
                {
                    var formatter =
                        (vt.GetTypeInfo().IsValueType ? typeof(FSharpOptionStructFormatter<,>) : typeof(FSharpOptionObjectFormatter<,>))
                            .MakeGenericType(resolverType, vt);
                    return Activator.CreateInstance(formatter);
                }
            }

            if (isGenericType && type.GetGenericTypeDefinition() == typeof(FSharpList<>))
            {
                var vt = type.GetTypeInfo().GetGenericArguments()[0];
                var formatter = typeof(FSharpListFormatter<,>).MakeGenericType(resolverType, vt);
                return Activator.CreateInstance(formatter);
            }

            if (isGenericType && type.GetGenericTypeDefinition() == typeof(FSharpMap<,>))
            {
                var vt = type.GetTypeInfo().GetGenericArguments();
                var formatter = typeof(FSharpMapFormatter<,,>).MakeGenericType(resolverType, vt[0], vt[1]);
                return Activator.CreateInstance(formatter);
            }

            if (isGenericType && type.GetGenericTypeDefinition() == typeof(FSharpSet<>))
            {
                var vt = type.GetTypeInfo().GetGenericArguments()[0];
                var formatter = typeof(FSharpSetFormatter<,>).MakeGenericType(resolverType, vt);
                return Activator.CreateInstance(formatter);
            }

            if (FSharpType.IsRecord(type, null))
            {
                return typeof(DynamicRecordFormatter).GetTypeInfo().GetMethod("Create")
                    .MakeGenericMethod(new[] { resolverType, type }).Invoke(null, null);
            }

            if (FSharpType.IsUnion(type, null))
            {
                return typeof(DynamicFSharpUnionFormatter).GetTypeInfo().GetMethod("Create")
                    .MakeGenericMethod(new[] { resolverType, type }).Invoke(null, null);
            }

            return Activator.CreateInstance(typeof(FSharpResolverFormatter<,>).MakeGenericType(typeof(TTypeResolver), type));
        }
    }

    internal class FSharpResolverFormatter<TTypeResolver, T> : Formatter<FSharpResolver<TTypeResolver>, T>
        where TTypeResolver : ITypeResolver, new()
    {
        private Formatter<TTypeResolver, T> innerFormatter;

        public FSharpResolverFormatter()
        {
            this.innerFormatter = Formatter<TTypeResolver, T>.Default;
        }

        public override T Deserialize(ref byte[] bytes, int offset, DirtyTracker tracker, out int byteSize)
        {
            return this.innerFormatter.Deserialize(ref bytes, offset, tracker, out byteSize);
        }

        public override int? GetLength()
        {
            return this.innerFormatter.GetLength();
        }

        public override int Serialize(ref byte[] bytes, int offset, T value)
        {
            return this.innerFormatter.Serialize(ref bytes, offset, value);
        }
    }
}
