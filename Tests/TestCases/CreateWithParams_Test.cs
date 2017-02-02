using System;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.TestCases
{
	[TestFixture]
	public class CreateWithParams_Test
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
			var b1 = serviceLocator.CreateB("test");
			Assert.That(b1, Is.Not.Null);
			Assert.That(b1.Value, Is.EqualTo("test"));
		}

		const string testAssemblyCode = @"
using ServiceLocatorKit;

public class B
{
	public B(string value)
	{
		Value = value;
	}
	public string Value;
}

public interface IServiceLocator
{
	B CreateB(string value);
}

[ImplementServiceLocator(typeof(IServiceLocator))]
public class ServiceLocator { }
";
	}
}