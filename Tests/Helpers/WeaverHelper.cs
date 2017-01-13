using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace Tests.Helpers
{
	public class WeaverHelper
	{
		public static object CreateClass(Assembly assembly, string classFullName)
		{
			var type = assembly.GetTypes().First(x => x.FullName == classFullName);
			return Activator.CreateInstance(type);
		}

		public static Assembly CompileAndWeave(string assemblyCode)
		{
			var assemblyPath = AssemblyCompiler.Compile(assemblyCode);
			WeaveAssembly(assemblyPath);
			return Assembly.LoadFile(assemblyPath);
		}

		public static void WeaveAssembly(string assemblyPath)
		{
			var moduleDefinition = ModuleDefinition.ReadModule(assemblyPath);
			var weavingTask = new ModuleWeaver
			{
				ModuleDefinition = moduleDefinition,
				LogInfo = x => Console.WriteLine(x)
			};
			weavingTask.Execute();
			moduleDefinition.Write(assemblyPath);
		}
	}
}