using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using ServiceLocatorKit;

namespace ServiceLocator.Fody.DependencyEngine
{
	public class Container
	{
		private readonly Dictionary<TypeReference, List<TypeDefinition>> interfaceImplmentationsMap = new Dictionary<TypeReference, List<TypeDefinition>>();

		public ModuleDefinition MainModule { get; }

		public Container(ModuleDefinition mainModule)
		{
			MainModule = mainModule;
		}

		public static Container Create(ModuleDefinition module)
		{
			var container = new Container(module);
			container.ImportModule(module);

			var additionalModules = GetAdditionalAssemblyNames(module)
				.Distinct()
				.Select(name => module.AssemblyResolver.Resolve(module.AssemblyReferences.First(ass => ass.Name == name)))
				.Select(x => x.MainModule);
			foreach (var additionalModule in additionalModules)
				container.ImportModule(additionalModule);

			return container;
		}

		private static IEnumerable<string> GetAdditionalAssemblyNames(ModuleDefinition module)
		{
			var attrName = typeof(SearchImplementationsInAttribute).FullName;
			return module.Assembly.CustomAttributes
				.Where(x => x.Constructor.DeclaringType.FullName == attrName)
				.SelectMany(x => (CustomAttributeArgument[]) x.ConstructorArguments.First().Value)
				.Select(a => (string)a.Value);
		}

		private void ImportModule(ModuleDefinition module)
		{
			foreach (var type in module.GetTypes())
			{
				var interfaces = type.Interfaces;
				foreach (var @interface in interfaces)
				{
					List<TypeDefinition> inheritors;
					if (!interfaceImplmentationsMap.TryGetValue(@interface, out inheritors))
					{
						inheritors = new List<TypeDefinition>();
						interfaceImplmentationsMap.Add(@interface, inheritors);
					}
					inheritors.Add(type);
				}
			}
		}

		public TypeDefinition FindImplementation(TypeReference queryType)
		{
			List<TypeDefinition> implementations;
			if (interfaceImplmentationsMap.TryGetValue(queryType, out implementations))
				return implementations.First();
			if (interfaceImplmentationsMap.TryGetValue(queryType.Resolve(), out implementations))
				return implementations.First();
			return null;
		}

		public static MethodDefinition ChooseConstructor(TypeDefinition type)
		{
			var publicConstructors = type
				.GetConstructors()
				.Where(x => x.IsPublic)
				.ToList();
			if (publicConstructors.Count == 1)
				return publicConstructors[0];
			throw new NotSupportedException($"Multiple constructors are not supported yet. ({type.FullName})");
		}
	}
}