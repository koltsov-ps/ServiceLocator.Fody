using System.Linq;
using System.Security.Policy;
using Mono.Cecil;
using ServiceLocator.Fody.GraphMechanics;

namespace ServiceLocator.Fody.DependencyEngine
{
	public class ImplementationNode : BaseNode, IGraphNode
	{
		public ImplementationNode(TypeDefinition type, CreationMethod creationMethod)
			: base(type)
		{
			CreationMethod = creationMethod;
		}

		public CreationMethod CreationMethod { get; }
		public Emission Emission { get; } = new Emission();
		public IGraphNode[] NextNodes => CreationMethod.Parameters.Cast<IGraphNode>().ToArray();
		public override string ToString() => Type.FullName;
	}
}