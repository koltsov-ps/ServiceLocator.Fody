using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ServiceLocator.Fody.GraphMechanics
{
	[Serializable]
	public class CyclicDependencyGraphException : GraphException
	{
		public static CyclicDependencyGraphException CreateFromCyclicPath(IEnumerable<IGraphNode> cyclicPath)
		{
			var message = new StringBuilder();
			var indent = "";
			foreach (var node in cyclicPath)
			{
				message.AppendLine($"{indent}{node}");
				indent += "  ";
			}
			return new CyclicDependencyGraphException($"Cyclic dependency:\n{message}");
		}

		public CyclicDependencyGraphException()
		{
		}

		public CyclicDependencyGraphException(string message) : base(message)
		{
		}

		public CyclicDependencyGraphException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected CyclicDependencyGraphException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}