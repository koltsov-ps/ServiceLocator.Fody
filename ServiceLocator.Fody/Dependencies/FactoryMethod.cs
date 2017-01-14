using Mono.Cecil;

namespace ServiceLocator.Fody.DependencyEngine
{
	public class FactoryMethod : CreationMethod
	{
		public FactoryMethod(MethodDefinition method) : base(method)
		{
		}

		public static FactoryMethod CreateFromCustomFactory(MethodDefinition method)
		{
			return new FactoryMethod(method);
		}
	}
}