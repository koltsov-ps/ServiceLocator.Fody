using System;

namespace ServiceLocatorKit
{
	[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false)]
	public class ContainerConstructorAttribute : Attribute
	{
	}
}