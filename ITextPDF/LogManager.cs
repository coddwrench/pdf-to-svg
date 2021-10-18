using System;

namespace IText.Logger
{

	public interface ILog
	{
		bool IsWarnEnabled { get; set; }
		bool IsErrorEnabled { get; set; }
		void Warn(string text);
		void Info(string text);
		void Debug(string text);
		void Error(string text, Exception e = null);
		void Trace(string text);
		
	}

	public static class LogManager
	{
		class EmptyLogger : ILog
		{
			public bool IsWarnEnabled { get; set; } = false;
			public bool IsErrorEnabled { get; set; } = false;
			public void Warn(string text) { }
			public void Info(string text) { }
			public void Debug(string text) { }
			public void Error(string text, Exception e = null) { }
			public void Trace(string text) { }
		}

		public static ILog GetLogger(Type t)
		{
			return new EmptyLogger();
		}
	}
}
