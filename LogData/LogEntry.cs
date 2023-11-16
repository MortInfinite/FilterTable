using System.Diagnostics;

namespace LogData
{
	/// <summary>
	/// Describes a single log message.
	/// </summary>
    [Serializable()]
	[DebuggerDisplay("TimeStamp = {TimeStamp}, LogLevelLevel = {LogLevel}, Message = {Message}")]
	public class LogEntry
	{
		/// <summary>
		/// ID identifying this log entry.
		/// </summary>
		public int Id
		{
			get; 
			set;
		}

		/// <summary>
		/// When the log entry was created.
		/// </summary>
		public DateTime? TimeStamp
		{
			get; 
			set;
		}

		/// <summary>
		/// The event id associated with the log.
		/// </summary>
		public int? EventId
		{
			get; 
			set;
		}

		/// <summary>
		/// Source of the log entry, indicating where it came from.
		/// </summary>
		public string Category
		{
			get; 
			set;
		}

		/// <summary>
		/// Severity of logged message.
		/// </summary>
		public string LogLevel
		{
			get;
			set;
		}

		/// <summary>
		/// Message to write in the log.
		/// </summary>
		public string Message
		{
			get;
			set;
		}

		/// <summary>
		/// The exception to log.
		/// </summary>
		public string? Exception
		{
			get; set;
		}

		/// <summary>
		/// Additional log information, such as call stack or documents.
		/// </summary>
		public string? Payload
		{
			get;
			set;
		}

		/// <summary>
		/// Type of data contained in the payload.
		/// </summary>
		public string? PayloadType
		{
			get;
			set;
		}
	}
}
