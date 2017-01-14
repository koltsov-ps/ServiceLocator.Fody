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
	public class MultiAssembly_Expected_Test
	{
		[Test]
		public void Test()
		{
			var firstAssemblyPath = AssemblyCompiler.Compile(firstAssemblyCode);
			var firstAssemblyName = Path.GetFileNameWithoutExtension(firstAssemblyPath);
			var secondAssemblyCodePatched = secondAssemblyCode.Replace("ASSM_REF", $"\"{firstAssemblyName}\"");
			var secondAssemblyPath = AssemblyCompiler.Compile(secondAssemblyCodePatched, Assembly.LoadFile(firstAssemblyPath));
			var module = ModuleDefinition.ReadModule(secondAssemblyPath);

			var firstAssemblyRefName = module.AssemblyReferences.First(x => x.Name == firstAssemblyName);
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

		[Test]
		public void Test2()
		{
			var firstAssemblyPath = AssemblyCompiler.Compile(firstAssemblyCode);
			var firstAssemblyName = Path.GetFileNameWithoutExtension(firstAssemblyPath);
			var secondAssemblyCodePatched = secondAssemblyCode.Replace("ASSM_REF", $"\"{firstAssemblyName}\"");
			var secondAssemblyPath = AssemblyCompiler.Compile(secondAssemblyCodePatched, Assembly.LoadFile(firstAssemblyPath));
			var module = ModuleDefinition.ReadModule(secondAssemblyPath);

			var firstAssemblyRefName = module.AssemblyReferences.First(x => x.Name == firstAssemblyName);
			var firstModule = module.AssemblyResolver
				.Resolve(firstAssemblyRefName)
				.MainModule;
			var cType = firstModule.GetType("C");
			var cTypeImported = module.ImportReference(cType);
			
			var bType = module.GetType("B");

			var serviceLocator = module.GetType("ServiceLocator");
			var method = new MethodDefinition("CreateA", MethodAttributes.HideBySig | MethodAttributes.Public, cTypeImported);
			var body = method.Body;
			var processor = body.GetILProcessor();
			var cCtor = cType.GetConstructors().First();
			var bCtor = bType.GetConstructors().First();
			processor.Emit(OpCodes.Newobj, module.ImportReference(bCtor));
			processor.Emit(OpCodes.Newobj, module.ImportReference(cCtor));
			processor.Emit(OpCodes.Ret);
			serviceLocator.Methods.Add(method);

			module.Write(secondAssemblyPath);
		}

		private const string firstAssemblyCode = @"
public interface IA {}
public interface IB {}

public class A : IA {}
public class C {
	public C(IB b) {}
}
";
		private const string secondAssemblyCode = 
@"using ServiceLocatorKit;
[assembly:SearchImplementationsIn(ASSM_REF)]
public class B: IB {}

public interface IServiceLocator
{
	IA A { get; }
	IB B { get; }
}

[ImplementServiceLocator(typeof(IServiceLocator))]
public class ServiceLocator { }";

	}
}