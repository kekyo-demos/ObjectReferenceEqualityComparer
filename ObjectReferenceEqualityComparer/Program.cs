////////////////////////////////////////////////////////////////////////////////////////////////////
//
// Copyright (c) Kouji Matsui, All rights reserved.
//
// * Redistribution and use in source and binary forms, with or without modification,
//   are permitted provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice,
//   this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
// IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
// INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
// EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace ObjectReferenceEqualityComparer
{
	public sealed class ObjectReferenceEqualityComparer : IEqualityComparer<object>, IEqualityComparer
	{
		private static readonly Func<object, int> getReferenceHashCode_;

		static ObjectReferenceEqualityComparer()
		{
			var assemblyName = new AssemblyName("ObjectReferenceAssembly");

			var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
				assemblyName,
				AssemblyBuilderAccess.Run);

			var moduleBuilder = assemblyBuilder.DefineDynamicModule(
				"ObjectReferenceModule",
				true);

			var typeBuilder = moduleBuilder.DefineType(
				"ObjectReference",
				TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Class);

			var methodBuilder = typeBuilder.DefineMethod(
				"GetReferenceHashCode",
				MethodAttributes.Public | MethodAttributes.Static,
				typeof(int),
				new[] { typeof(object) });

			var generator = methodBuilder.GetILGenerator();

			var objectGetHashCodeMethod = typeof(object).GetMethod("GetHashCode");

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Call, objectGetHashCodeMethod);
			generator.Emit(OpCodes.Ret);

			var type = typeBuilder.CreateType();

			var getReferenceHashCodeMethod = type.GetMethod(methodBuilder.Name);

			getReferenceHashCode_ = (Func<object, int>)Delegate.CreateDelegate(
				typeof(Func<object, int>),
				getReferenceHashCodeMethod);
		}

		public new bool Equals(object x, object y)
		{
			return object.ReferenceEquals(x, y);
		}

		public int GetHashCode(object obj)
		{
			return getReferenceHashCode_(obj);
		}
	}

	public static class Program
	{
		public static void Main(string[] args)
		{
			var equalityComparer = new ObjectReferenceEqualityComparer();

			var hashSet = new HashSet<TestClass>(equalityComparer);

			for (var index = 0; index < 10000000; index++)
			{
				var test = new TestClass();
				hashSet.Add(test);
			}

			Debug.Assert(hashSet.Count == 10000000);
		}

		public sealed class TestClass
		{
			public TestClass()
			{
			}

			public override bool Equals(object obj)
			{
				return true;
			}

			public override int GetHashCode()
			{
				return 123;
			}

			public int ObjectReferenceGetHashCode()
			{
				return base.GetHashCode();
			}
		}
	}
}
