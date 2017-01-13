using System;
using NUnit.Framework;
using ServiceLocator.Fody.GraphMechanics;
using Tests.Helpers;

namespace Tests.TestCases
{
	[TestFixture]
	public class Errors_Test
	{
		[Test]
		public void CyclicDependency()
		{
			try
			{
				WeaverHelper.CompileAndWeave(cyclicDependancyExample);
				Assert.Fail();
			}
			catch (CyclicDependencyGraphException e)
			{
				Func<string, string> normalize = str => str.Trim().Replace("\r", "");
				Assert.That(normalize(e.Message), Is.EqualTo(normalize(cyclicDependancyExceptionMessage)));
			}
		}

		private const string cyclicDependancyExceptionMessage = @"
Cyclic dependency:
IA
  A
    IB
      B
        IA";
		private const string cyclicDependancyExample = @"
using ServiceLocatorKit;

public interface IA { }
public interface IB { }

public class A : IA
{
	public A(IB b) { }
}
public class B : IB
{
	public B(IA a) { }
}

public interface IServiceLocator
{
	IA GetA();
	IB GetB();
}

[ImplementServiceLocator(typeof(IServiceLocator))]
public class ServiceLocator { }";
	}
}