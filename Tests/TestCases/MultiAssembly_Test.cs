using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.TestCases
{
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
public interface IC {}
public class A : IA {}
public class C : IC {
	public C(IB b){}
}
";
		private const string secondAssemblyCode = 
"using ServiceLocatorKit;"+
"[assembly:SearchImplementationsIn(\"tmp_FirstAssembly\")]"+
@"public class B: IB {}

public interface IServiceLocator
{
	IA A { get; }
	IB B { get; }
	IC C { get; }
}

[ImplementServiceLocator(typeof(IServiceLocator))]
public class ServiceLocator { }";
	}
}