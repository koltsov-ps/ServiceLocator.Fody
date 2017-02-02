using System.Collections.Generic;
using Mono.Cecil;
using ServiceLocator.Fody.GraphMechanics;

namespace ServiceLocator.Fody.DependencyEngine
{
	public class QueryNode : BaseNode, IQueryNode
	{
		public QueryNode(TypeReference type, ImplementationNode impl)
			: base(type)
		{
			Impl = impl;
		}

		public ImplementationNode Impl { get; }
		public Emission Emission { get; } = new Emission();
		public IGraphNode[] NextNodes => new IGraphNode[] { Impl };
		public List<PropertyDefinition> Properties { get; } = new List<PropertyDefinition>();
		public override string ToString() => Type.FullName;
	}
}