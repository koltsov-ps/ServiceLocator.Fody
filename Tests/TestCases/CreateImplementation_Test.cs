using System;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.TestCases
{
	[TestFixture]
	public class CreateImplementation_Test
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
			var b2 = serviceLocator.CreateB();
			Assert.That(b1, Is.Not.Null);
			Assert.That(b2, Is.Not.Null);
			Assert.That(!Object.ReferenceEquals(b1, b2));
			var a1 = b1.GetA();
			var a2 = b2.GetA();
			Assert.That(a1, Is.Not.Null);
			Assert.That(a2, Is.Not.Null);
			Assert.That(Object.ReferenceEquals(a1, a2));
		}

		const string testAssemblyCode = @"
using ServiceLocatorKit;

public interface IA { }
public interface IB { }

public class A : IA { }
public class B : IB
{
	private IA a;
	public B(IA a)
	{
		this.a = a;
	}
	public IA GetA() => a;
}

public interface IServiceLocator
{
	IB CreateB();
}

[ImplementServiceLocator(typeof(IServiceLocator))]
public class ServiceLocator { }
";
	}
}