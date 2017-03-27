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
	private TypeDefinition type;
	public void Emit(TypeDefinition type, TypeDefinition interfaceType, DependencyGraph graph)
	{
		this.type = type;
		var nodesToGenerate = graph.TopSort();
		type.Interfaces.Add(interfaceType);
		foreach (var node in nodesToGenerate)
		{
			EmitNode(node);
		}
		//orphanSetters
		foreach (var property in interfaceType.Properties)
		{
			if (property.GetMethod != null)
				continue;
			var propertyFromType = type.Properties.FirstOrDefault(x => string.Equals(x.Name, property.Name, StringComparison.InvariantCulture));
			if (propertyFromType != null && propertyFromType.SetMethod != null)
			{
				propertyFromType.SetMethod.Attributes = defaultGetterMethodAttributes | MethodAttributes.SpecialName;
			}
			else if (propertyFromType != null)
			{
				var prop = EmitOrphanSetter(property);
				propertyFromType.SetMethod = prop.SetMethod;
				type.Methods.Add(prop.SetMethod);
			}
			else
			{
				var prop = EmitOrphanSetter(property);
				type.Properties.Add(prop);
				type.Methods.Add(prop.SetMethod);
			}
		}
	}

	private void EmitNode(IGraphNode node)
	{
		var implNode = node as ImplementationNode;
		if (implNode != null)
		{
			var methodAndIsEmmitedList = EmitCreateMethods(implNode)
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
		if (queryNode != null && queryNode.Emission.GetterMethod == null)
		{
			var emittedField = queryNode.Impl.Emission.EmitedField;
			var field = emittedField ?? EmitField(type, queryNode);
			var methods = EmitGetters(queryNode, field)
				.ToList();
			if (emittedField == null)
			{
				type.Fields.Add(field);
				queryNode.Impl.Emission.EmitedField = field;
			}
			foreach (var method in methods)
				type.Methods.Add(method);

			var props = EmitProperties(queryNode, field)
				.ToList();
			foreach (var prop in props)
			{
				type.Properties.Add(prop);
				type.Methods.Add(prop.GetMethod);
				if (prop.SetMethod != null)
					type.Methods.Add(prop.SetMethod);
			}

			//TODO: if only property exists, default getter will be generated
			queryNode.Emission.GetterMethod = methods.FirstOrDefault() ?? props.FirstOrDefault()?.GetMethod;
		}
		else if (queryNode != null && queryNode.Emission.GetterMethod != null)
		{
			//foreach (var property in queryNode.Properties)
			//{
				queryNode.Emission.GetterMethod.Attributes = defaultGetterMethodAttributes | MethodAttributes.SpecialName;
			//}
		}
	}

	private IEnumerable<Tuple<MethodDefinition, bool>> EmitCreateMethods(ImplementationNode implNode)
	{
		if (implNode.Prototypes.Count > 0)
		{
			foreach (var method in implNode.Prototypes)
			{
				yield return EmitImplementation(implNode, method);
			}
		}
		else
		{
			yield return EmitImplementation(implNode);
		}
	}

	private MethodDefinition CreateMethodDefinitionFromPrototype(MethodDefinition prototype)
	{
		var method = new MethodDefinition(prototype.Name, defaultGetterMethodAttributes, prototype.ReturnType);
		foreach (var parameter in prototype.Parameters)
			method.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, parameter.ParameterType));
		return method;
	}

	private MethodDefinition CreateMethodDefinition(string prefix, BaseNode implNode)
	{
		var returnType = implNode.ImportTypeIfNeeded(type.Module);
		return new MethodDefinition( $"{prefix}{implNode.Type.Name}", MethodAttributes.Private | MethodAttributes.HideBySig, returnType);
	}

	private Tuple<MethodDefinition, bool> EmitImplementation(ImplementationNode implNode, MethodDefinition methodPrototype = null)
	{
		var factoryMethod = implNode.CreationMethod as FactoryMethod;
		if (factoryMethod != null && !factoryMethod.Method.HasParameters)
			return Tuple.Create(factoryMethod.Method, false);

		var method = methodPrototype != null
			? CreateMethodDefinitionFromPrototype(methodPrototype)
			: CreateMethodDefinition("Create", implNode);
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

	private void EmitMethodParameters(ILProcessor processor, List<IQueryNode> parameters)
	{
		foreach (var parameter in parameters)
		{
			var queryNode = parameter as QueryNode;
			if (queryNode != null)
			{
				var getterMethod = queryNode.Emission.GetterMethod;
				if (getterMethod == null)
					throw new InvalidOperationException("Getter method not found");
				if (!getterMethod.IsStatic)
					processor.Emit(OpCodes.Ldarg_0);
				processor.Emit(OpCodes.Call, getterMethod);
			}
			var parameterNode = parameter as ParameterNode;
			if (parameterNode != null)
			{
				var seq = parameterNode.Parameter.Sequence;
				if (seq == 1)
					processor.Emit(OpCodes.Ldarg_1);
				else if (seq == 2)
					processor.Emit(OpCodes.Ldarg_2);
				else if (seq == 3)
					processor.Emit(OpCodes.Ldarg_3);
				else
					processor.Emit(OpCodes.Ldarg_S, seq);
			}
		}
	}

	private IEnumerable<PropertyDefinition> EmitProperties(QueryNode queryNode, FieldDefinition field)
	{
		return queryNode.Properties
			.Select(p => EmitProperty(queryNode, p.Name, p, field));
	}

	private IEnumerable<MethodDefinition> EmitGetters(QueryNode queryNode, FieldDefinition field)
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
			var method = CreateMethodDefinition("Get", queryNode);
			EmitGetterBody(queryNode, method, field);
			yield return method;
		}
	}

	private PropertyDefinition EmitProperty(QueryNode node, string name, PropertyDefinition prototypeProperty, FieldDefinition field)
	{
		var pref = "get_";
		var pureName = name.StartsWith(pref)
			? name.Substring(pref.Length)
			: name;
		var returnType = prototypeProperty.GetMethod.ReturnType;
		var property = new PropertyDefinition(pureName, PropertyAttributes.None, returnType);
		property.GetMethod = new MethodDefinition("get_"+pureName, defaultGetterMethodAttributes | MethodAttributes.SpecialName, returnType);
		EmitGetterBody(node, property.GetMethod, field);
		if (prototypeProperty.SetMethod != null)
		{
			var setter = new MethodDefinition("set_"+pureName, defaultGetterMethodAttributes | MethodAttributes.SpecialName, prototypeProperty.SetMethod.ReturnType);
			setter.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, returnType));
			property.SetMethod = setter;
			EmitSetterBody(node, setter, field);
		}
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

	private void EmitSetterBody(QueryNode node, MethodDefinition method, FieldDefinition field)
	{
		var body = method.Body;
		var processor = body.GetILProcessor();
		processor.Emit(OpCodes.Ldarg_0);
		processor.Emit(OpCodes.Ldarg_1);
		processor.Emit(OpCodes.Castclass, field.FieldType);
		processor.Emit(OpCodes.Stfld, field);
		processor.Emit(OpCodes.Ret);
	}

	private PropertyDefinition EmitOrphanSetter(PropertyDefinition prototypeProperty)
	{
		var pureName = prototypeProperty.Name;
		var paramType = prototypeProperty.SetMethod.Parameters[0].ParameterType;
		var property = new PropertyDefinition(pureName, PropertyAttributes.None, paramType);
		if (prototypeProperty.SetMethod != null)
		{
			var setter = new MethodDefinition("set_"+pureName, defaultGetterMethodAttributes | MethodAttributes.SpecialName, prototypeProperty.SetMethod.ReturnType);
			setter.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, paramType));
			property.SetMethod = setter;
			var body = setter.Body;
			var processor = body.GetILProcessor();
			processor.Emit(OpCodes.Ret);
		}
		return property;
	}

	private FieldDefinition EmitField(TypeDefinition type, QueryNode queryNode)
	{
		var implType = queryNode.Impl.ImportTypeIfNeeded(type.Module);
		var name = $"serviceLocator_{implType.Name}";
		return new FieldDefinition(name, FieldAttributes.Private, implType);
	}
}