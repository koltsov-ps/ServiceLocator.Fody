using System.Collections.Generic;
using Mono.Cecil;

namespace ServiceLocator.Fody.DependencyEngine
{
	public class BaseNode
	{
		protected BaseNode(TypeReference type)
		{
			Type = type;
		}

		public TypeReference Type { get; }

		public TypeReference ImportTypeIfNeeded(ModuleDefinition module)
		{
			return module != Type.Module
				? module.ImportReference(Type)
				: Type;
		}

		public List<MethodDefinition> Prototypes { get; } = new List<MethodDefinition>();
	}
}