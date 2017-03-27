using NUnit.Framework;
using Tests.Helpers;

namespace Tests.TestCases
{
	[TestFixture]
	public class ReuseProperties_Test
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
			var b1 = serviceLocator.Prop;
			Assert.That(b1, Is.Not.Null);
			var v = (int)b1.GetValue();
			Assert.That(v, Is.EqualTo(123));
		}

		const string testAssemblyCode = @"
using System;
using ServiceLocatorKit;

public class B
{
	private int value;
	public B(int value)
	{
		this.value = value;
	}

	public int GetValue() => value;
}

public interface IServiceLocator
{
	B Prop { get; }
}

[ImplementServiceLocator(typeof(IServiceLocator))]
public class ServiceLocator {
	public B Prop { get { return new B(123); } }

	private static IServiceLocator instance;
	public static IServiceLocator Default => instance ?? (instance = (IServiceLocator)new ServiceLocator());
}
";
	}
}