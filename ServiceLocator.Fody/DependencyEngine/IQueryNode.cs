using Mono.Cecil;
using ServiceLocator.Fody.GraphMechanics;

namespace ServiceLocator.Fody.DependencyEngine
{
	public interface IQueryNode : IGraphNode
	{
		TypeReference Type { get; }
	}
}