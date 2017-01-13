using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using NUnit.Framework;
using ServiceLocatorKit;

namespace Tests.Helpers
{
	public static class AssemblyCompiler
	{
		private static readonly Assembly[] defaultAssemblies =
		{
			typeof (ImplementServiceLocatorAttribute).Assembly
		};

		static AssemblyCompiler()
		{
			var roslynBinDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin");
			var targetDir = Path.Combine(roslynBinDir, "roslyn");
			var sourceDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "roslyn");
			if (!Directory.Exists(roslynBinDir))
				Directory.CreateDirectory(roslynBinDir);
			if (!Directory.Exists(targetDir))
			{
				Directory.Move(sourceDir, targetDir);
			}
			CleanupTestAssemblies();
			//AppDomain.CurrentDomain.DomainUnload += delegate { CleanupTestAssemblies(); };
		}

		public static string Compile(string source, params Assembly[] references)
		{
			return Compile(source, null, references);
		}

		public static string Compile(string source, string resultFileName, params Assembly[] references)
		{
			var compilationParameters = new CompilerParameters
			{
				OutputAssembly = resultFileName ?? "tmp_" + Guid.NewGuid().ToString("N") + ".dll",
				GenerateExecutable = false
			};
			foreach (var reference in references.Concat(defaultAssemblies).Select(x => x.GetName().Name + ".dll"))
				compilationParameters.ReferencedAssemblies.Add(reference);
			CompilerResults compilationResult;
			using (var codeDomProvider = new CSharpCodeProvider())
			{
				compilationResult = codeDomProvider.CompileAssemblyFromSource(compilationParameters, source);
			}
			if (compilationResult.Errors.HasErrors || compilationResult.Errors.HasWarnings)
			{
				var message = string.Join("\r\n", compilationResult.Errors
					.Cast<CompilerError>()
					.Select(x => $"{x.Line}:{x.Column} {x.ErrorText}"));
				Assert.Fail(message);
			}

			return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, compilationResult.PathToAssembly);
		}

		private static void CleanupTestAssemblies()
		{
			var testAssemblyFileNames = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory)
				.Where(x => Path.GetFileName(x).StartsWith("tmp"));
			foreach (var fileName in testAssemblyFileNames)
				try
				{
					File.Delete(fileName);
				}
				catch (IOException)
				{
				}
		}
	}
}