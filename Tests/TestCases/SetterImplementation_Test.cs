using System.Reflection;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.TestCases
{
	[TestFixture]
	public class SetterImplementation_Test
	{
		private dynamic serviceLocator;
		private Assembly assembly;

		[TestFixtureSetUp]
		public void Setup()
		{
			assembly = WeaverHelper.CompileAndWeave(testAssemblyCode);
			serviceLocator = WeaverHelper.CreateClass(assembly, "ServiceLocator");
		}

		[Test]
		public void Get_Set()
		{
			var a1 = serviceLocator.A;
			Assert.That(a1, Is.Not.Null);
			var a2 = (dynamic) WeaverHelper.CreateClass(assembly, "A");
			serviceLocator.A = a2;
			Assert.That(serviceLocator.A, Is.EqualTo(a2));
			Assert.That(serviceLocator.A, Is.Not.EqualTo(a1));
		}

		[Test]
		public void Set_Get()
		{
			var a = (dynamic) WeaverHelper.CreateClass(assembly, "A");
			serviceLocator.A = a;
			Assert.That(serviceLocator.A, Is.EqualTo(a));
		}

		[Test]
		public void Get_Set_Interface()
		{
			var a1 = serviceLocator.IA;
			Assert.That(a1, Is.Not.Null);
			var a2 = (dynamic) WeaverHelper.CreateClass(assembly, "A");
			serviceLocator.IA = a2;
			Assert.That(serviceLocator.IA, Is.EqualTo(a2));
			Assert.That(serviceLocator.IA, Is.Not.EqualTo(a1));
		}

		[Test]
		public void Set_Get_Interface()
		{
			var a = (dynamic) WeaverHelper.CreateClass(assembly, "A");
			serviceLocator.IA = a;
			Assert.That(serviceLocator.IA, Is.EqualTo(a));
		}

		[Test]
		public void Orphan_Set()
		{
			serviceLocator.Orphan = new object();
		}

		[Test]
		public void ReUseExistedProperties()
		{
			Assert.That(serviceLocator.B2, Is.Null);
			Assert.That(serviceLocator.B3, Is.Null);
			Assert.That(serviceLocator.B4, Is.Null);

			var b = (dynamic) WeaverHelper.CreateClass(assembly, "B");
			serviceLocator.B1 = b;
			serviceLocator.B2 = b;
			serviceLocator.B3 = b;
			serviceLocator.B4 = b;
			Assert.That(serviceLocator.B1, Is.EqualTo(serviceLocator.B2));
		}

		const string testAssemblyCode = @"
using ServiceLocatorKit;

public interface IA { }
public class A : IA { }
public class B {}

public interface IServiceLocator
{
	IA IA {get; set;}
	A A {get; set;}
	object Orphan { set; }
	B B1 { set; }
	B B2 { get; set; }
	B B3 { get; }
	B B4 { set; }
}

[ImplementServiceLocator(typeof(IServiceLocator))]
public class ServiceLocator {
	public B B1 { get; set; }
	public B B2 { get; set; }
	public B B3 { get; set; }
	public B B4 { get; }
}
";
	}
}