using System.Collections.Generic;

namespace ServiceLocator.Fody.GraphMechanics
{
	public static class GraphTopSort
	{
		public static IGraphNode[] TopSort(this IEnumerable<IGraphNode> entryNodes)
		{
			var searchState = new Dictionary<IGraphNode, int>();
			var result = new List<IGraphNode>();
			foreach (var node in entryNodes)
				VisitNode(node, searchState, result, new List<IGraphNode> { node });
			return result.ToArray();
		}

		private static void VisitNode(IGraphNode node, Dictionary<IGraphNode, int> searchState, List<IGraphNode> result, List<IGraphNode> path)
		{
			int nodeState;
			const int inProgress = 1;
			if (searchState.TryGetValue(node, out nodeState))
			{
				if (nodeState == inProgress)
					throw CyclicDependencyGraphException.CreateFromCyclicPath(path);
				return;
			}
			searchState[node] = inProgress;
			foreach (var child in node.NextNodes)
			{
				path.Add(child);
				VisitNode(child, searchState, result, path);
				path.RemoveAt(path.Count - 1);
			}
			result.Add(node);
			const int visited = 2;
			searchState[node] = visited;
		}
	}
}