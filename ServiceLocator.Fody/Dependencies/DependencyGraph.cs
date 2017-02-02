using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using ServiceLocator.Fody.GraphMechanics;
using ServiceLocator.Fody.Utils;

namespace ServiceLocator.Fody.DependencyEngine
{
	public class DependencyGraph
	{
		private readonly Container container;
		private readonly ILog log;
		private List<IGraphNode> entryPoints = new List<IGraphNode>();
		private List<IGraphNode> allNodes = new List<IGraphNode>();

		private Dictionary<TypeReference, QueryNode> queryNodes = new Dictionary<TypeReference, QueryNode>();
		private Dictionary<TypeDefinition, ImplementationNode> implNodes = new Dictionary<TypeDefinition, ImplementationNode>();

		public DependencyGraph(Container container, ILog log)
		{
			this.container = container;
			this.log = log;
		}

		public ReadOnlyCollection<IGraphNode> EntryPoints => new ReadOnlyCollection<IGraphNode>(entryPoints);

		public QueryNode AddQueryEntry(TypeReference queryType)
		{
			var queryNode = GetOrCreateQueryNode(queryType);
			entryPoints.Add(queryNode);
			return queryNode;
		}

		public ImplementationNode AddCreateEntry(MethodDefinition method)
		{
			var implType = method.ReturnType;
			var impl = FindImpl(implType);
			ImplementationNode implNode;
			var implNodeAdded = TryAddImplementation(impl, ConstructorMethod.ChooseConstructor, out implNode);
			if (implNodeAdded)
				TraverceCreationMethodParameters(implNode, method);
			entryPoints.Add(implNode);
			return implNode;
		}

		private QueryNode GetOrCreateQueryNode(TypeReference queryType)
		{
			QueryNode queryNode;
			if (queryNodes.TryGetValue(queryType, out queryNode))
				return queryNode;
			if (queryNodes.TryGetValue(queryType.Resolve(), out queryNode))
				return queryNode;
			if (queryType.IsArray)
				throw new NotSupportedException($"Array type is not supported yet. {queryType.FullName}");
			ImplementationNode implNode;
			var impl = FindImpl(queryType);

			var implNodeAdded = TryAddImplementation(impl, ConstructorMethod.ChooseConstructor, out implNode);
			queryNode = new QueryNode(queryType, implNode);
			queryNodes.Add(queryType, queryNode);
			allNodes.Add(queryNode);

			if (implNodeAdded)
				TraverceCreationMethodParameters(implNode, null);
			return queryNode;
		}

		private TypeDefinition FindImpl(TypeReference queryType)
		{
			var impl = container.FindImplementation(queryType);
			ImplementationNode implNode;
			if (impl == null)
			{
				if (queryType.IsPrimitive)
					throw new InvalidOperationException($"Primitive types are not supported {queryType.FullName}");
				var queryTypeDefinition = queryType.Resolve();

				if (queryTypeDefinition.IsAbstract && !implNodes.TryGetValue(queryTypeDefinition, out implNode))
					throw new InvalidOperationException($"Implementation not found for {queryType.FullName}");
				impl = queryTypeDefinition;
			}
			return impl;
		}

		public IGraphNode[] TopSort() => entryPoints.TopSort();

		public void AddCustomFactoryMethod(MethodDefinition factoryMethod)
		{
			var typeDefinition = factoryMethod.ReturnType.Resolve();
			ImplementationNode implNode;
			log.Info($"Add custom factory: {typeDefinition.Name} => {factoryMethod}");
			if (TryAddImplementation(typeDefinition, type => FactoryMethod.CreateFromCustomFactory(factoryMethod), out implNode))
				TraverceCreationMethodParameters(implNode, null);
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
		private void TraverceCreationMethodParameters(ImplementationNode implNode, MethodDefinition prototypeMethod)
		{
			Dictionary<string, ParameterNode> dict = null;
			if (prototypeMethod != null)
			{
				dict = prototypeMethod.Parameters
					.ToDictionary(x => x.Name, x => new ParameterNode(x));
			}
			var parameters = implNode.CreationMethod.Method.Parameters;
			foreach (var parameter in parameters)
			{
				ParameterNode parameterNode = null;
				dict?.TryGetValue(parameter.Name, out parameterNode);
				var queryNode = (IQueryNode) parameterNode ?? GetOrCreateQueryNode(parameter.ParameterType);
				implNode.CreationMethod.Parameters.Add(queryNode);
			}
		}
	}
}