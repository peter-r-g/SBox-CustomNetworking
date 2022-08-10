using System;

namespace CustomNetworking.Shared.Utility;

/// <summary>
/// An abstraction layer for logging.
/// </summary>
public static class Logging
{
	/// <summary>
	/// Logs information.
	/// </summary>
	/// <param name="message">The message to log.</param>
	public static void Info( string message )
	{
#if SERVER
		var oldColor = Console.ForegroundColor;
		Console.ForegroundColor = ConsoleColor.Green;
		
		Console.WriteLine( $"[{DateTime.Now}] [INFO]: {message}" );
		
		Console.ForegroundColor = oldColor;
#endif
#if CLIENT
		Log.Info( message );
#endif
	}

	/// <summary>
	/// Logs a warning.
	/// </summary>
	/// <param name="message">The message to log.</param>
	/// <param name="exception">The <see cref="Exception"/> attached to this warning.</param>
	public static void Warning( string message, Exception? exception = null )
	{
#if SERVER
		var oldColor = Console.ForegroundColor;
		Console.ForegroundColor = ConsoleColor.Yellow;
		
		Console.WriteLine( $"[{DateTime.Now}] [WARN]: {message}" );
		if ( exception is not null )
			Console.WriteLine( exception );
		
		Console.ForegroundColor = oldColor;
#endif
#if CLIENT
		Log.Warning( exception, message );
#endif
	}

	/// <summary>
	/// Logs a warning.
	/// </summary>
	/// <param name="exception">The exception to log.</param>
	public static void Warning( Exception exception )
	{
#if SERVER
		var oldColor = Console.ForegroundColor;
		Console.ForegroundColor = ConsoleColor.Yellow;
		
		Console.WriteLine( $"[{DateTime.Now}] [WARN]: {exception}" );
		
		Console.ForegroundColor = oldColor;
#endif
#if CLIENT
		Log.Warning( exception );
#endif
	}

	/// <summary>
	/// Logs an error.
	/// </summary>
	/// <param name="message">The message to log.</param>
	/// <param name="exception">The <see cref="Exception"/> attached to this error.</param>
	public static void Error( string message, Exception? exception = null )
	{
#if SERVER
		var oldColor = Console.ForegroundColor;
		Console.ForegroundColor = ConsoleColor.Red;
		
		Console.WriteLine( $"[{DateTime.Now}] [ERR]: {message}" );
		if ( exception is not null )
			Console.WriteLine( exception );
		
		Console.ForegroundColor = oldColor;
#endif
#if CLIENT
		Log.Error( exception, message );
#endif
	}

	/// <summary>
	/// Logs an error.
	/// </summary>
	/// <param name="exception">The exception to log.</param>
	public static void Error( Exception exception )
	{
#if SERVER
		var oldColor = Console.ForegroundColor;
		Console.ForegroundColor = ConsoleColor.Red;
		
		Console.WriteLine( $"[{DateTime.Now}] [ERR]: {exception}" );
		
		Console.ForegroundColor = oldColor;
#endif
#if CLIENT
		Log.Error( exception );
#endif
	}
}
