using System;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.TestCases
{
	[TestFixture]
	public class MultiConstructors_Test
	{
		private dynamic serviceLocator;

		[TestFixtureSetUp]
		public void Setup()
		{
			var assembly = WeaverHelper.CompileAndWeave(testAssemblyCode);
			serviceLocator = WeaverHelper.CreateClass(assembly, "ServiceLocator");
		}

		[Test]
		public void Test()
		{
			var b1 = serviceLocator.CreateB();
			Assert.That(b1, Is.Not.Null);
			var a1 = b1.GetA();
			Assert.That(a1, Is.Not.Null);
		}

		const string testAssemblyCode = @"
using System;
using ServiceLocatorKit;

public class A {}
public class B
{
	private A a;
	public B(A a)
	{
		this.a = a;
		throw new Exception();
	}

	[ContainerConstructor]
	public B(A a1, A a2)
	{
		this.a = a1;
		if (a1 != a2)
			throw new ArgumentException();
	}
	public A GetA() => a;
}

public interface IServiceLocator
{
	B CreateB();
}

[ImplementServiceLocator(typeof(IServiceLocator))]
public class ServiceLocator { }
";
	}
}