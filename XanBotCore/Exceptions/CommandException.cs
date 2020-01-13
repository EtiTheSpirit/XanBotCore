using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using XanBotCore.CommandSystem;
using XanBotCore.CommandSystem.Commands.ArchonCommands;

namespace XanBotCore.Exceptions {

	/// <summary>
	/// Represents an error that occurs due to improper command usage, like incorrect input or invalid arguments.
	/// </summary>
	public class CommandException : Exception {

		/// <summary>
		/// The error message sent by the command.
		/// </summary>
		public override string Message { get; }

		/// <summary>
		/// The command that threw this error.
		/// </summary>
		public Command Command { get; }

		/// <summary>
		/// True if the command message should be deleted, in which case the amount of time is specified by <see cref="ExpireTimeMilliseconds"/>
		/// </summary>
		public bool Expires { get; }

		/// <summary>
		/// The amount of milliseconds until the sent <see cref="DiscordMessage"/> reflecting this command is deleted, if applicable.
		/// </summary>
		public int ExpireTimeMilliseconds { get; } = 0;

		/// <summary>
		/// Create a new CommandException with the specified command source, error message, and optional expire time in milliseconds (which deletes the message sent in the <see cref="DiscordChannel"/> the command was executed in, if applicable.)
		/// </summary>
		/// <param name="source">The command that threw this error. Should always be <see cref="this"/></param>
		/// <param name="errMsg">The error message to accompany which describes why this exception was thrown.</param>
		/// <param name="expireTimeMilliseconds">The amount of milliseconds until the message sent is deleted. This does nothing if this command was executed from the console.</param>
		public CommandException(Command source, string errMsg, int? expireTimeMilliseconds = null) {
			Command = source;
			Message = errMsg;
			Expires = expireTimeMilliseconds != null;
			if (Expires) {
				ExpireTimeMilliseconds = expireTimeMilliseconds.Value;
			}
		}

		public CommandException(Command source, Exception subExc, int? expireTimeMilliseconds = null) {
			Command = source;
			Message = $"**[{subExc.GetType().FullName} thrown!]** {subExc.Message}";
			Expires = expireTimeMilliseconds != null;
			if (Expires) {
				ExpireTimeMilliseconds = expireTimeMilliseconds.Value;
			}
		}
	}
}
