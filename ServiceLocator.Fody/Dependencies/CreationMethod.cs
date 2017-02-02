using System.Collections.Generic;
using Mono.Cecil;

namespace ServiceLocator.Fody.DependencyEngine
{
	public class CreationMethod
	{
		protected CreationMethod(MethodDefinition method)
		{
			Method = method;
		}

		public MethodDefinition Method { get; }
		public List<IQueryNode> Parameters { get; } = new List<IQueryNode>();

		public MethodReference ImportMethodIfNeeded(ModuleDefinition module)
		{
			return module != Method.Module
				? module.ImportReference(Method)
				: Method;
		}
	}
}