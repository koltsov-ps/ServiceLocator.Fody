using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using NUnit.Framework;
using Tests.Helpers;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace Tests.TestCases
{
	[TestFixture]
	public class ASDF
	{
		[Test]
		public void Test()
		{
			var firstAssemblyPath = AssemblyCompiler.Compile(firstAssemblyCode, "tmp_FirstAssembly.dll");
			var secondAssemblyPath = AssemblyCompiler.Compile(secondAssemblyCode, Assembly.LoadFile(firstAssemblyPath));
			var module = ModuleDefinition.ReadModule(secondAssemblyPath);

			var firstAssemblyRefName = module.AssemblyReferences.First(x => x.Name == "tmp_FirstAssembly");
			var firstModule = module.AssemblyResolver
				.Resolve(firstAssemblyRefName)
				.MainModule;
			var aType = firstModule.GetType("A");
			var aTypeImported = module.ImportReference(aType);
			

			var serviceLocator = module.GetType("ServiceLocator");
			var method = new MethodDefinition("CreateA", MethodAttributes.HideBySig | MethodAttributes.Public, aTypeImported);
			var body = method.Body;
			var processor = body.GetILProcessor();
			var constructor = aType.GetConstructors().First();
			processor.Emit(OpCodes.Newobj, module.ImportReference(constructor));
			processor.Emit(OpCodes.Ret);
			serviceLocator.Methods.Add(method);

			module.Write(secondAssemblyPath);
		}

		private const string firstAssemblyCode = @"
public interface IA {}
public interface IB {}

public class A : IA {}
";
		private const string secondAssemblyCode = 
"using ServiceLocatorKit;"+
"[assembly:SearchImplementationsIn(\"tmp_FirstAssembly\")]"+
@"public class B: IB {}

public interface IServiceLocator
{
	IA A { get; }
	IB B { get; }
}

[ImplementServiceLocator(typeof(IServiceLocator))]
public class ServiceLocator { }";
	}

	[TestFixture]
	public class MultiAssembly_Test
	{
		private dynamic serviceLocator;

		[TestFixtureSetUp]
		public void Setup()
		{
			var firstAssemblyPath = AssemblyCompiler.Compile(firstAssemblyCode, "tmp_FirstAssembly.dll");
			var secondAssemblyPath = AssemblyCompiler.Compile(secondAssemblyCode, Assembly.LoadFile(firstAssemblyPath));
			WeaverHelper.WeaveAssembly(secondAssemblyPath);
			var assembly = Assembly.LoadFile(secondAssemblyPath);
			serviceLocator = WeaverHelper.CreateClass(assembly, "ServiceLocator");
		}

		[Test]
		public void Test()
		{
			var a = serviceLocator.A;
			Assert.That(a, Is.Not.Null);
			var b = serviceLocator.B;
			Assert.That(b, Is.Not.Null);
		}

		private const string firstAssemblyCode = @"
public interface IA {}
public interface IB {}

public class A : IA {}
";
		private const string secondAssemblyCode = 
"using ServiceLocatorKit;"+
"[assembly:SearchImplementationsIn(\"tmp_FirstAssembly\")]"+
@"public class B: IB {}

public interface IServiceLocator
{
	IA A { get; }
	IB B { get; }
}

[ImplementServiceLocator(typeof(IServiceLocator))]
public class ServiceLocator { }";
	}
}