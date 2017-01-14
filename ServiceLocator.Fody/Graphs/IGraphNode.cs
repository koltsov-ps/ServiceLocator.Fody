namespace ServiceLocator.Fody.GraphMechanics
{
	public interface IGraphNode
	{
		IGraphNode[] NextNodes { get; }
	}
}