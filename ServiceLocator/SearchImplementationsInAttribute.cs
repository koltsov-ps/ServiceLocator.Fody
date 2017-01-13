using System;

namespace ServiceLocatorKit
{
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	public class SearchImplementationsInAttribute : Attribute
	{
		public SearchImplementationsInAttribute(params string[] assemblyNames)
		{
			AssemblyNames = assemblyNames;
		}

		public string[] AssemblyNames { get; set; }
	}
}