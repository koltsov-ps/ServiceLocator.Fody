using Mono.Cecil;
using ServiceLocator.Fody.GraphMechanics;

namespace ServiceLocator.Fody.DependencyEngine
{
	public class QueryNode : IQueryNode
	{
		public QueryNode(TypeReference type, ImplementationNode impl)
		{
			Type = type;
			Impl = impl;
		}

		public TypeReference Type { get; }
		public ImplementationNode Impl { get; }

		public Emission Emission { get; set; } = new Emission();

		public IGraphNode[] NextNodes => new IGraphNode[] { Impl };

		public override string ToString() => Type.FullName;
	}
}