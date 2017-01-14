using Mono.Cecil;

namespace ServiceLocator.Fody.DependencyEngine
{
	public class Emission
	{
		public FieldDefinition EmitedField;
		public MethodDefinition GetterMethod;
		public MethodDefinition FactoryMethod;
	}
}