using System;

namespace ServiceLocatorKit
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class ImplementServiceLocatorAttribute : Attribute
	{
		public ImplementServiceLocatorAttribute(Type interfaceType)
		{
			InterfaceType = interfaceType;
		}

		public Type InterfaceType { get; set; }
	}
}