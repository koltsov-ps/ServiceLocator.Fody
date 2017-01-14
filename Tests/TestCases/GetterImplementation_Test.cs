using System;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.TestCases
{
	[TestFixture]
	public class GetterImplementation_Test
	{
		private dynamic serviceLocator;

		[TestFixtureSetUp]
		public void Setup()
		{
			var assembly = WeaverHelper.CompileAndWeave(testAssemblyCode);
			serviceLocator = WeaverHelper.CreateClass(assembly, "ServiceLocator");
		}

		[Test]
		public void EmptyConstructor()
		{
			var a = serviceLocator.GetA();
			Assert.That(a, Is.Not.Null);
			Assert.That(a.GetType().FullName, Is.EqualTo("A"));
			var a2 = serviceLocator.GetA();
			Assert.That(Object.ReferenceEquals(a, a2));
		}

		[Test]
		public void ResolveConstructorParameters()
		{
			var b = serviceLocator.GetB();
			Assert.That(b, Is.Not.Null);
			Assert.That(b.GetType().FullName, Is.EqualTo("B"));
			var b2 = serviceLocator.GetB();
			Assert.That(Object.ReferenceEquals(b, b2));
			var a = b.GetA();
			Assert.That(a, Is.Not.Null);
		}

		[Test]
		public void GetConcreteClass()
		{
			var c1 = serviceLocator.GetC();
			Assert.That(c1, Is.Not.Null);
			Assert.That(c1.GetType().FullName, Is.EqualTo("C"));
			var c2 = serviceLocator.GetC();
			Assert.That(Object.ReferenceEquals(c1, c2));
		}

		const string testAssemblyCode = @"
using ServiceLocatorKit;

public interface IA { }
public interface IB { }
public interface IC { }
public interface ID { }

public class A : IA { }
public class B : IB
{
	private IA a;
	public B(IA a, IC c, ID d)
	{
		this.a = a;
	}

	public IA GetA() => a;
}

public class C : IC { }
public class D : ID { }

public interface IServiceLocator
{
	IA GetA();
	IB GetB();
	C GetC();
}

[ImplementServiceLocator(typeof(IServiceLocator))]
public class ServiceLocator { }
";
	}
}