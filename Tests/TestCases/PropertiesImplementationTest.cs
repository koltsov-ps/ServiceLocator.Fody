using System;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.TestCases
{
	[TestFixture]
	public class PropertiesImplementationTest
	{
		private dynamic serviceLocator;

		[TestFixtureSetUp]
		public void Setup()
		{
			var assembly = WeaverHelper.CompileAndWeave(testAssemblyCode);
			serviceLocator = WeaverHelper.CreateClass(assembly, "ServiceLocator");
		}

		[Test]
		public void Property()
		{
			var b = serviceLocator.B;
			Assert.That(b, Is.Not.Null);
			var a = b.A;
			Assert.That(a, Is.Not.Null);
		}

		[Test]
		public void PropertyAndMethodReturnsSameResult()
		{
			var c1 = serviceLocator.GetC();
			var c2 = serviceLocator.C;
			Assert.That(c1, Is.Not.Null);
			Assert.That(c2, Is.Not.Null);
			Assert.That(Object.ReferenceEquals(c1, c2));
		}

		const string testAssemblyCode = @"
using ServiceLocatorKit;

public interface IA { }
public interface IB { }
public interface IC { }

public class A : IA { }
public class B : IB
{
	public B(IA a)
	{
		A = a;
	}
	public IA A { get; }
}
public class C : IC { }

public interface IServiceLocator
{
	IB B { get; }
	IC C { get; }
	IC GetC();
}

[ImplementServiceLocator(typeof(IServiceLocator))]
public class ServiceLocator { }";
	}
}