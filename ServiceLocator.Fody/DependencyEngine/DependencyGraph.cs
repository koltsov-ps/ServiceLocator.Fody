using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;
using ServiceLocator.Fody.GraphMechanics;

namespace ServiceLocator.Fody.DependencyEngine
{
	public class DependencyGraph
	{
		private readonly Container container;
		private List<QueryNode> entryPoints = new List<QueryNode>();
		private List<IGraphNode> allNodes = new List<IGraphNode>();

		private Dictionary<TypeReference, QueryNode> queryNodes = new Dictionary<TypeReference, QueryNode>();
		private Dictionary<TypeDefinition, ImplementationNode> implNodes = new Dictionary<TypeDefinition, ImplementationNode>();

		public DependencyGraph(Container container)
		{
			this.container = container;
		}

		public ReadOnlyCollection<QueryNode> EntryPoints => new ReadOnlyCollection<QueryNode>(entryPoints);

		public QueryNode AddEntryPoint(TypeReference queryType)
		{
			var queryNode = GetOrCreateQueryNode(queryType);
			entryPoints.Add(queryNode);
			return queryNode;
		}

		private QueryNode GetOrCreateQueryNode(TypeReference queryType)
		{
			QueryNode queryNode;
			if (queryNodes.TryGetValue(queryType, out queryNode))
				return queryNode;
			if (queryType.IsArray)
				throw new NotSupportedException($"Array type is not supported yet. {queryType.FullName}");
			var impl = container.FindImplementation(queryType);
			if (impl == null)
			{
				if (queryType.IsPrimitive)
					throw new InvalidOperationException($"Primitive types are not supported {queryType.FullName}");
				var queryTypeDefinition = queryType.Resolve();
				if (queryTypeDefinition.IsAbstract)
					throw new InvalidOperationException($"Implementation not found for {queryType.FullName}");
				impl = queryTypeDefinition;
			}
			ImplementationNode implNode;

			var implNodeAdded = TryAddImplementation(impl, ConstructorMethod.ChooseConstructor, out implNode);
			queryNode = new QueryNode(queryType, implNode);
			queryNodes.Add(queryType, queryNode);
			allNodes.Add(queryNode);

			if (implNodeAdded)
				TraverceCreationMethodParameters(implNode);
			return queryNode;
		}

		public IGraphNode[] TopSort() => entryPoints.TopSort();

		public void AddCustomFactoryMethod(MethodDefinition factoryMethod)
		{
			var typeDefinition = factoryMethod.ReturnType.Resolve();
			ImplementationNode implNode;
			if (TryAddImplementation(typeDefinition, type => FactoryMethod.CreateFromCustomFactory(factoryMethod), out implNode))
				TraverceCreationMethodParameters(implNode);
		}

		private bool TryAddImplementation(TypeDefinition typeDefinition, Func<TypeDefinition, CreationMethod> getCreationMethod, out ImplementationNode implNode)
		{
			if (implNodes.TryGetValue(typeDefinition, out implNode))
				return false;
			implNode = new ImplementationNode(typeDefinition, getCreationMethod(typeDefinition));
			implNodes.Add(typeDefinition, implNode);
			allNodes.Add(implNode);
			return true;
		}
		private void TraverceCreationMethodParameters(ImplementationNode implNode)
		{
			var parameters = implNode.CreationMethod.Method.Parameters;
			foreach (var parameter in parameters)
			{
				var parameterNode = GetOrCreateQueryNode(parameter.ParameterType);
				implNode.CreationMethod.Parameters.Add(parameterNode);
			}
		}
	}
}