using System;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.TestCases
{
	[TestFixture]
	public class CustomConstructor_Test
	{
		private dynamic serviceLocator;

		[TestFixtureSetUp]
		public void Setup()
		{
			var assembly = WeaverHelper.CompileAndWeave(testAssemblyCode);
			serviceLocator = WeaverHelper.CreateClass(assembly, "ServiceLocator");
		}

		[Test]
		public void StaticFactory()
		{
			var a = serviceLocator.A;
			Assert.That(a, Is.Not.Null);
			Assert.That(a.Value, Is.EqualTo(1));
		}

		[Test]
		public void InstanceFactory()
		{
			var c = serviceLocator.C;
			Assert.That(c, Is.Not.Null);
			Assert.That(c.Value, Is.EqualTo(3));
		}

		[Test]
		public void InstanceFactoryWithArgs()
		{
			var b = serviceLocator.B;
			Assert.That(b, Is.Not.Null);
			Assert.That(b.Value, Is.EqualTo(2));
			var a = serviceLocator.A;
			Assert.That(a, Is.Not.Null);
			Assert.That(Object.ReferenceEquals(b.A, a));
		}

		const string testAssemblyCode = @"
using ServiceLocatorKit;

public class A{
	public A(int value){
		Value = value;
	}
	public int Value {get;}
}
public class B
{
	public B(A a, int value)
	{
		A = a;
		Value = value;
	}
	public A A { get; }
	public int Value { get; }
}
public class C {
	public C(int value){
		Value = value;
	}
	public int Value {get;}
}

public interface IServiceLocator
{
	A A { get; }
	B B { get; }
	C C { get; }
}

[ImplementServiceLocator(typeof(IServiceLocator))]
public class ServiceLocator {
	private static A CreateStaticA() { return new A(1); }
	private B CreateInstanceB(A a) { return new B(a, 2); }
	private C CreateInstanceC() { return new C(3); }
}";
	}
}