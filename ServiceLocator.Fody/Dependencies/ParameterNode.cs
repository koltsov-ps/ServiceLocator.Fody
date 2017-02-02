using Mono.Cecil;
using ServiceLocator.Fody.GraphMechanics;

namespace ServiceLocator.Fody.DependencyEngine
{
	public class ParameterNode : IQueryNode
	{
		public ParameterNode(ParameterDefinition parameter)
		{
			Parameter = parameter;
			Type = parameter.ParameterType;
		}

		public ParameterDefinition Parameter { get; }
		public TypeReference Type { get; }
		public IGraphNode[] NextNodes { get; } = new IGraphNode[0];
		public override string ToString() => $"parameter {Parameter.Name}: {Type.FullName}";
	}
}