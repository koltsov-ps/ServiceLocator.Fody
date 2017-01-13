using System.Linq;
using System.Security.Policy;
using Mono.Cecil;
using ServiceLocator.Fody.GraphMechanics;

namespace ServiceLocator.Fody.DependencyEngine
{
	public class ImplementationNode : IGraphNode
	{
		public ImplementationNode(TypeDefinition type, CreationMethod creationMethod)
		{
			Type = type;
			CreationMethod = creationMethod;
		}

		public TypeDefinition Type { get; }

		public TypeReference ImportTypeIfNeeded(ModuleDefinition module)
		{
			return module != Type.Module
				? module.ImportReference(Type)
				: Type;
		}

		public CreationMethod CreationMethod { get; }
		public Emission Emission { get; } = new Emission();
		public IGraphNode[] NextNodes => CreationMethod.Parameters.Cast<IGraphNode>().ToArray();
		public override string ToString() => Type.FullName;
	}
}