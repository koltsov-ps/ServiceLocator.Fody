using System;

namespace ServiceLocator.Fody.Utils
{
	public class Log : ILog
	{
		private readonly Action<string> logInfo;

		public Log(Action<string> logInfo)
		{
			this.logInfo = logInfo;
		}

		public void Info(string message)
			=> logInfo(message);
	}
}