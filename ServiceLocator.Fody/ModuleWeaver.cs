using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using ServiceLocator.Fody.DependencyEngine;
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
		var container = Container.Create(ModuleDefinition);
		
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
		var dependencyGraph = new DependencyGraph(container);

		foreach (var method in type.Methods)
		{
			if (!method.Name.StartsWith("Create"))
				continue;
			dependencyGraph.AddCustomFactoryMethod(method);
		}
		

		foreach (var interfaceMethod in interfaceDef.Methods)
			dependencyGraph.AddEntryPoint(interfaceMethod.ReturnType);
		new Emitter().Emit(type, interfaceDef, dependencyGraph);
	}
}