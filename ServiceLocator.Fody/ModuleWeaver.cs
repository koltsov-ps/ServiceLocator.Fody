using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using ServiceLocator.Fody.DependencyEngine;
using ServiceLocator.Fody.Utils;
using ServiceLocatorKit;

public partial class ModuleWeaver
{
	public ModuleWeaver()
	{
		LogInfo = m => { };
	}

	public Action<string> LogInfo { get; set; }
	public ModuleDefinition ModuleDefinition { get; set; }

	public void Execute()
	{
		FindTargetClasses();
	}
}

public partial class ModuleWeaver
{
	public class ServiceLocatorConfig
	{
		public TypeDefinition[] Interfaces { get; }
		public string[] Assemblies { get; }
	}
	private static IEnumerable<TypeDefinition> HasServiceLocatorAttributes(TypeDefinition type)
	{
		var attrName = typeof(ImplementServiceLocatorAttribute).FullName;
		return type.CustomAttributes
			.Where(x => x.Constructor.DeclaringType.FullName == attrName)
			.Select(x => (TypeDefinition) x.ConstructorArguments.First().Value);
	}


	public void FindTargetClasses()
	{
		var allTypes = ModuleDefinition.GetTypes().ToList();
		var container = Container.Create(ModuleDefinition, new Log(LogInfo));
		
		var serviceLocators = allTypes
			.Select(x => new { Type = x, Requires = HasServiceLocatorAttributes(x).ToList() })
			.Where(x => x.Requires.Count > 0)
			.ToList();

		foreach (var serviceLocator in serviceLocators)
			foreach (var require in serviceLocator.Requires)
			{
				ImplementServiceLocator(serviceLocator.Type, require, container);
			}
	}

	private void ImplementServiceLocator(TypeDefinition type, TypeDefinition interfaceDef, Container container)
	{
		var dependencyGraph = new DependencyGraph(container, new Log(LogInfo));

		foreach (var method in type.Methods)
		{
			if (!method.Name.StartsWith("Create"))
				continue;
			dependencyGraph.AddCustomFactoryMethod(method);
		}

		foreach (var property in type.Properties)
		{
			dependencyGraph.TryAddCustomProperty(property);
		}

		var properties = interfaceDef.Properties;
		var propertiesMethods = properties.SelectMany(x =>
			{
				var methods = new List<MethodDefinition>();
				if (x.GetMethod != null)
					methods.Add(x.GetMethod);
				if (x.SetMethod != null)
					methods.Add(x.SetMethod);
				return methods;
			})
			.ToList();

		foreach (var interfaceMethod in interfaceDef.Methods.Except(propertiesMethods))
		{
			if (interfaceMethod.Name.StartsWith("Create", StringComparison.InvariantCultureIgnoreCase))
			{
				var implNode = dependencyGraph.AddCreateEntry(interfaceMethod);
				implNode.Prototypes.Add(interfaceMethod);
			}
			else
			{
				var queryNode = dependencyGraph.AddQueryEntry(interfaceMethod.ReturnType);
				queryNode.Prototypes.Add(interfaceMethod);
			}
		}

		foreach (var property in interfaceDef.Properties)
		{
			var getMethod = property.GetMethod;
			if (getMethod != null)
			{
				var queryNode = dependencyGraph.AddQueryEntry(getMethod.ReturnType);
				queryNode.Prototypes.Add(getMethod);
				queryNode.Properties.Add(property);
			}
		}

		new Emitter().Emit(type, interfaceDef, dependencyGraph);
	}


}