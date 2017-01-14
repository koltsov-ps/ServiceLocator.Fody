using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using ServiceLocator.Fody.DependencyEngine;
using ServiceLocator.Fody.GraphMechanics;

public class Emitter
{
	private MethodAttributes defaultGetterMethodAttributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.VtableLayoutMask;

	public void Emit(TypeDefinition type, TypeDefinition interfaceType, DependencyGraph graph)
	{
		var nodesToGenerate = graph.TopSort();
		type.Interfaces.Add(interfaceType);
		foreach (var node in nodesToGenerate)
		{
			EmitNode(type, node);
		}
	}

	private void EmitNode(TypeDefinition type, IGraphNode node)
	{
		var implNode = node as ImplementationNode;
		if (implNode != null)
		{
			var methodAndIsEmmitedList = EmitCreateMethods(type, implNode)
				.ToList();
			foreach (var methodAndIsEmmited in methodAndIsEmmitedList)
			{
				var method = methodAndIsEmmited.Item1;
				var isEmmited = methodAndIsEmmited.Item2;
				implNode.Emission.FactoryMethod = method;
				if (isEmmited)
					type.Methods.Add(method);
			}
			return;
		}
		var queryNode = node as QueryNode;
		if (queryNode != null)
		{
			var field = EmitField(type, queryNode);
			var methods = EmitGetters(type, queryNode, field)
				.ToList();
			type.Fields.Add(field);
			foreach (var method in methods)
				type.Methods.Add(method);

			var props = EmitProperties(queryNode, field)
				.ToList();
			foreach (var prop in props)
			{
				type.Properties.Add(prop);
				type.Methods.Add(prop.GetMethod);
			}

			//TODO: if only property exists, default getter will be generated
			queryNode.Emission.GetterMethod = methods.FirstOrDefault() ?? props.FirstOrDefault()?.GetMethod;
		}
	}

	private IEnumerable<Tuple<MethodDefinition, bool>> EmitCreateMethods(TypeDefinition type, ImplementationNode implNode)
	{
		if (implNode.Prototypes.Count > 0)
		{
			foreach (var method in implNode.Prototypes)
			{
				yield return EmitImplementation(type, implNode, method);
			}
		}
		else
		{
			yield return EmitImplementation(type, implNode);
		}
	}

	private MethodDefinition CreateMethodDefinitionFromPrototype(MethodDefinition prototype)
	{
		return new MethodDefinition(prototype.Name, defaultGetterMethodAttributes, prototype.ReturnType);
	}

	private MethodDefinition CreateMethodDefinition(string prefix, TypeDefinition type, BaseNode implNode)
	{
		var returnType = implNode.ImportTypeIfNeeded(type.Module);
		return new MethodDefinition( $"{prefix}{implNode.Type.Name}", MethodAttributes.Private | MethodAttributes.HideBySig, returnType);
	}

	private Tuple<MethodDefinition, bool> EmitImplementation(TypeDefinition type, ImplementationNode implNode, MethodDefinition methodPrototype = null)
	{
		var factoryMethod = implNode.CreationMethod as FactoryMethod;
		if (factoryMethod != null && !factoryMethod.Method.HasParameters)
			return Tuple.Create(factoryMethod.Method, false);

		var method = methodPrototype != null
			? CreateMethodDefinitionFromPrototype(methodPrototype)
			: CreateMethodDefinition("Create", type, implNode);
		var body = method.Body;
		var processor = body.GetILProcessor();

		var constructorMethod = implNode.CreationMethod as ConstructorMethod;
		if (constructorMethod != null)
		{
			EmitMethodParameters(processor, constructorMethod.Parameters);
			var constructor = constructorMethod.ImportMethodIfNeeded(type.Module);
			processor.Emit(OpCodes.Newobj, constructor);
		}
		if (factoryMethod != null)
		{
			if (!factoryMethod.Method.IsStatic)
				processor.Emit(OpCodes.Ldarg_0);
			EmitMethodParameters(processor, factoryMethod.Parameters);
			processor.Emit(OpCodes.Call, factoryMethod.Method);
		}

		processor.Emit(OpCodes.Ret);
		return Tuple.Create(method, true);
	}

	private void EmitMethodParameters(ILProcessor processor, List<QueryNode> parameters)
	{
		foreach (var parameter in parameters)
		{
			var getterMethod = parameter.Emission.GetterMethod;
			if (getterMethod == null)
				throw new InvalidOperationException("Getter method not found");
			if (!getterMethod.IsStatic)
				processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Call, getterMethod);
		}
	}

	private IEnumerable<PropertyDefinition> EmitProperties(QueryNode queryNode, FieldDefinition field)
	{
		return queryNode.Prototypes
			.Where(m => m.IsGetter)
			.Select(m => EmitProperty(queryNode, m.Name, m.ReturnType, field));
	}

	private IEnumerable<MethodDefinition> EmitGetters(TypeDefinition type, QueryNode queryNode, FieldDefinition field)
	{
		if (queryNode.Prototypes.Count > 0)
		{
			foreach (var prototypeMethod in queryNode.Prototypes.Where(x => !x.IsGetter && !x.IsSetter))
			{
				var method = CreateMethodDefinitionFromPrototype(prototypeMethod);
				EmitGetterBody(queryNode, method, field);
				yield return method;
			}
		}
		else
		{
			var method = CreateMethodDefinition("Get", type, queryNode);
			EmitGetterBody(queryNode, method, field);
			yield return method;
		}
	}

	private PropertyDefinition EmitProperty(QueryNode node, string name, TypeReference returnType, FieldDefinition field)
	{
		var pref = "get_";
		var pureName = name.StartsWith(pref)
			? name.Substring(pref.Length)
			: name;
		var property = new PropertyDefinition(pureName, PropertyAttributes.None, returnType);
		property.GetMethod = new MethodDefinition(name, defaultGetterMethodAttributes | MethodAttributes.SpecialName, returnType);
		EmitGetterBody(node, property.GetMethod, field);
		return property;
	}

	private void EmitGetterBody(QueryNode node, MethodDefinition method, FieldDefinition field)
	{
		var factoryMethod = node.Impl.Emission.FactoryMethod;
		if (factoryMethod == null)
			throw new InvalidOperationException("Factory method not found");
		
		var body = method.Body;
		body.Variables.Add(new VariableDefinition("V_0", method.ReturnType));
		var processor = body.GetILProcessor();
		var ret = processor.Create(OpCodes.Ret);
		processor.Emit(OpCodes.Ldarg_0);
		processor.Emit(OpCodes.Ldfld, field);
		processor.Emit(OpCodes.Dup);
		processor.Emit(OpCodes.Brtrue_S, ret);
		processor.Emit(OpCodes.Pop);
		processor.Emit(OpCodes.Ldarg_0);
		if (!factoryMethod.IsStatic)
			processor.Emit(OpCodes.Ldarg_0);
		processor.Emit(OpCodes.Call, factoryMethod);
		processor.Emit(OpCodes.Dup);
		processor.Emit(OpCodes.Stloc_0);
		processor.Emit(OpCodes.Stfld, field);
		processor.Emit(OpCodes.Ldloc_0);
		processor.Append(ret);
	}

	private FieldDefinition EmitField(TypeDefinition type, QueryNode queryNode)
	{
		var implType = queryNode.Impl.ImportTypeIfNeeded(type.Module);
		var name = $"serviceLocator_{implType.Name}";
		return new FieldDefinition(name, FieldAttributes.Private, implType);
	}
}