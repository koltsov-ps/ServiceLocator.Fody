using System;
using Mono.Cecil;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.TestCases
{
	[TestFixture]
	public class ExpectedGeneration_Test
	{
		
		[Test]
		public void PropertyGetter()
		{
			CompileAndDump(@"
public interface IClass {
	object A { get; }
}
public class Class : IClass {
	private object a;
	public object A {
		get {
			return a ?? (a = new object());
		}
	}
	public object B {
		get {
			return a ?? (a = new object());
		}
	}
}");
		}

		[Test]
		public static void Method()
		{
			CompileAndDump(@"
public interface IA { }
public class A : IA { }
public interface IServiceLocator
{
	IA GetA();
}
public class ServiceLocator : IServiceLocator
{
	private IA a;
	public IA GetA() => a ?? (a = new A());
}");
		}

		[Test]
		public static void StaticFactories()
		{
			CompileAndDump(@"
public class A { }
public class B {
	public B (A a){}
}
public class ServiceLocator
{
	private A a;
	private B b;
	public static A CreateStaticA() => new A();
	public A CreateInstanceA() => new A();
	public static B CreateStaticB(A a) => new B(a);
	public B CreateInstanceB(A a) => new B(a);
	public void UseStaticA() => a = CreateStaticA();
	public void UseInstanceA() => a = CreateInstanceA();
	public void UseStaticB() => b = CreateStaticB(a);
	public void UseInstanceB() => b = CreateInstanceB(a);
}");
		}

		[Test]
		public static void PropertySetter()
		{
			CompileAndDump(@"

public class Class {
	private object b;
	private Class c;
	public object B { set { b = value; } }
	public object C { set { c = (Class) value; } }
	public object D { set { } }
}");
		}

		public static void CompileAndDump(string sourceCode)
		{
			var assemblyPath = AssemblyCompiler.Compile(sourceCode);
			var moduleDefinition = ModuleDefinition.ReadModule(assemblyPath);
			foreach (var type in moduleDefinition.GetTypes())
				Dump(type);
		}

		private static void Dump(TypeDefinition type)
		{
			Console.WriteLine($"Type: {type.FullName}");
			foreach (var method in type.Methods)
			{
				Console.WriteLine();
				Console.WriteLine($"METHOD {method.Name}");
				Dump(method);
			}
		}

		private static void Dump(MethodDefinition method)
		{
			Console.WriteLine($"Attributes: {method.Attributes}");
			if (!method.HasBody)
				return;
			if (method.Body.HasVariables)
			{
				Console.WriteLine("Variables:");
				foreach (var variable in method.Body.Variables)
					Console.WriteLine($"* {variable}");
			}
			Console.WriteLine("Body");
			foreach (var instruction in method.Body.Instructions)
			{
				Console.WriteLine(instruction.ToString());
			}
		}
	}
}