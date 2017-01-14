using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using ServiceLocator.Fody.Utils;
using ServiceLocatorKit;

namespace ServiceLocator.Fody.DependencyEngine
{
	public class Container
	{
		private readonly HashSet<ModuleDefinition> modules;
		private ILog log;
		private readonly Dictionary<TypeReference, List<TypeDefinition>> interfaceImplmentationsMap = new Dictionary<TypeReference, List<TypeDefinition>>();

		public ModuleDefinition MainModule { get; }

		public Container(ModuleDefinition mainModule, HashSet<ModuleDefinition> modules, ILog log)
		{
			MainModule = mainModule;
			this.modules = modules;
			this.log = log;
		}

		public static Container Create(ModuleDefinition module, ILog log)
		{
			var additionalModules = GetAdditionalAssemblyNames(module)
				.Distinct()
				.Select(name => module.AssemblyResolver.Resolve(module.AssemblyReferences.First(ass => ass.Name == name)))
				.SelectMany(x => x.Modules)
				.ToList();
			var modules = new HashSet<ModuleDefinition>();
			modules.Add(module);
			foreach (var additionalModule in additionalModules)
				modules.Add(additionalModule);

			var container = new Container(module, modules, log);
			container.ImportModule(module);
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
					var resolvedInterface = modules.Contains(@interface.Module)
						? @interface.Resolve()
						: @interface;

					if (!interfaceImplmentationsMap.TryGetValue(resolvedInterface, out inheritors))
					{
						inheritors = new List<TypeDefinition>();
						interfaceImplmentationsMap.Add(resolvedInterface, inheritors);
					}
					log.Info($"Add implementation {type.Name} for interface {resolvedInterface.Name}");
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