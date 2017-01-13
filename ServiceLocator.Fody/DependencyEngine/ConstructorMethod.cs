using Mono.Cecil;

namespace ServiceLocator.Fody.DependencyEngine
{
	public class ConstructorMethod : CreationMethod
	{
		public ConstructorMethod(MethodDefinition method) : base(method)
		{
		}

		public static CreationMethod ChooseConstructor(TypeDefinition type)
		{
			return new ConstructorMethod(Container.ChooseConstructor(type));
		}
	}
}