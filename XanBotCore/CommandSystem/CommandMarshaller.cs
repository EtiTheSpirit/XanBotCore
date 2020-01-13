using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XanBotCore.CommandSystem.Commands;
using XanBotCore.CommandSystem.Commands.ArchonCommands;
using XanBotCore.Exceptions;
using XanBotCore.Logging;
using XanBotCore.ServerRepresentation;
using XanBotCore.UserObjects;
using XanBotCore.Utility;
using XanBotCore.PassiveHandlers;

namespace XanBotCore.CommandSystem {

	/// <summary>
	/// Controls the execution and flow of commands initiated by the bot.
	/// </summary>
	public class CommandMarshaller {

		/// <summary>The command prefix that must be used in the Discord chat in order to issue commands.</summary>
		public static string CommandPrefix { get; set; } = ">>";

		/// <summary>Set to true if the prefix can have spaces after it. Default value is true.</summary>
		public static bool AllowSpacesAfterPrefix { get; set; } = true;

		/// <summary>Stores whether or not the get method of Commands has sorted the command array in order from lowest permissions to highest permissions.</summary>
		private static bool HasSortedCommands = false;
		private static bool HasSortedArchonCommands = false;

		/// <summary>The internal array of available commands in this bot. Any commands not specified here will not be usable during runtime.</summary>
		private static Command[] CommandsInternal = new Command[] {
			new CommandArchonCommand(),
			new CommandConfig(),
			new CommandGetPerms(),
			new CommandHelp(),
			new CommandListHandlers(),
			new CommandSetPerms(),
			new CommandShutdown(),
		};
		private static ArchonCommand[] ArchonCommandsInternal = new ArchonCommand[] {
			new ArchonCommandHelp(),
			new ArchonCommandCurrentContext(),
			new ArchonCommandForcePerms(),
			new ArchonCommandToggleDebugLogging(),
		};

		private static Command[] MergedCommands = CommandsInternal;
		private static ArchonCommand[] MergedArchonCommands = ArchonCommandsInternal;

		/// <summary>
		/// The array of available commands in this bot.<para/>
		/// This includes both stock commands offered as part of XanBotCore, as well as all user-defined commands in <see cref="UserGlobalCommands"/>.<para/>
		/// This sorts the commands by their required permission level, and of two or more commands have the same level, sorts them by name alphabetically.
		/// </summary> 
		public static Command[] Commands {
			get {
				if (!HasSortedCommands) {
					XanBotLogger.WriteDebugLine("Commands array referenced, but it hasn't been sorted. Sorting now...");
					MergedCommands = new Command[CommandsInternal.Length + UserCommandsInternal.Length];
					XanBotLogger.WriteDebugLine($"Found {MergedCommands.Length} commands.");
					int idx = 0;
					foreach (Command stock in CommandsInternal) {
						MergedCommands[idx] = stock;
						idx++;
					}
					foreach (Command user in UserCommandsInternal) {
						MergedCommands[idx] = user;
						idx++;
					}
					Array.Sort(MergedCommands);
					HasSortedCommands = true;
				}
				return MergedCommands;
			}
		}

		/// <summary>
		/// The array of available archon commands in this bot.<para/>
		/// This includes both stock archon commands offered as part of XanBotCore, as well as all user-defined archon commands in <see cref="UserArchonCommands"/>.<para/>
		/// This sorts the archon commands by name alphabetically.
		/// </summary> 
		public static ArchonCommand[] ArchonCommands {
			get {
				if (!HasSortedArchonCommands) {
					XanBotLogger.WriteDebugLine("ArchonCommands array referenced, but it hasn't been sorted. Sorting now...");
					MergedArchonCommands = new ArchonCommand[ArchonCommandsInternal.Length + UserArchonCommandsInternal.Length];
					XanBotLogger.WriteDebugLine($"Found {MergedCommands.Length} archon commands.");
					int idx = 0;
					foreach (ArchonCommand stock in ArchonCommandsInternal) {
						MergedArchonCommands[idx] = stock;
						idx++;
					}
					foreach (ArchonCommand user in UserArchonCommandsInternal) {
						MergedArchonCommands[idx] = user;
						idx++;
					}
					Array.Sort(MergedArchonCommands);
					HasSortedArchonCommands = true;
				}
				return MergedArchonCommands;
			}
		}


		private static Command[] UserCommandsInternal = new Command[0];
		private static ArchonCommand[] UserArchonCommandsInternal = new ArchonCommand[0];
		/// <summary>
		/// An array of all user-specified global commands. If a command only needs to exist in one server, use <see cref="BotContext.ContextSpecificCommands"/> instead.<para/>
		/// Reference <see cref="Commands"/> for an array storing both stock commands and all user-specified global commands.
		/// </summary>
		public static Command[] UserGlobalCommands {
			get {
				return UserCommandsInternal;
			}
			set {
				UserCommandsInternal = value;
				HasSortedCommands = false;
			}
		}

		/// <summary>
		/// An array of all user-specified global archon commands. Contrary to stock <see cref="Command"/> objects, <see cref="ArchonCommand"/>s can NOT exist in specific contexts as they apply to low level bot controls.<para/>
		/// Reference <see cref="ArchonCommands"/> for an array storing both stock archon commands and all user-specified archon commands.
		/// </summary>
		public static ArchonCommand[] UserArchonCommands {
			get {
				return UserArchonCommandsInternal;
			}
			set {
				UserArchonCommandsInternal = value;
				HasSortedArchonCommands = false;
			}
		}

		/// <summary>
		/// Handles a command sent by the underlying console, which is input as a string.<para/>
		/// !!! This should not be used for commands issued via Discord messages !!!
		/// </summary>
		/// <param name="text">The text input to the console.</param>
		public static async Task HandleCommand(string text) {
			if (text == "") {
				return;
			}
			string[] allArgs = ArgumentSplitter.SplitArgs(text);
			string command = allArgs[0];
			string[] args = new string[0];
			if (allArgs.Length > 1) {
				args = allArgs.Skip(1).ToArray();
			}
			Command execCommand = null;
			string cmdLower = command.ToLower();
			foreach (Command cmd in Commands) {
				if (cmd.Name.ToLower() == cmdLower) {
					execCommand = cmd;
					break;
				} else {
					if (cmd.AlternateNames != null) {
						foreach (string altName in cmd.AlternateNames) {
							if (altName.ToLower() == cmdLower) {
								execCommand = cmd;
								break;
							}
						}
					}
				}
			}
			if (execCommand != null) {
				try {
					string allArgsText = "";
					if (args.Length > 0) {
						allArgsText = text.Substring(command.Length + 1);
					}
					await execCommand.ExecuteCommandAsync(null, null, null, args, allArgsText);
				} catch (CommandException commandErr) {
					string message = string.Format("§cFailed to issue command \"{0}\": §1{1}", commandErr.Command.Name, commandErr.Message);
					XanBotLogger.WriteLine(message);
				} catch (ArchonCommandException commandErr) {
					string message = string.Format("§cFailed to issue Archon Command \"{0}\": §1{1}", commandErr.Command.Name, commandErr.Message);
					XanBotLogger.WriteLine(message);
				}
			} else {
				XanBotLogger.WriteLine("§1The command `" + command + "` does not exist.");
			}
		}

		/// <summary>
		/// Handles a command issued via a Discord message. This does not test if it is a command. Please test with <see cref="IsCommand(string)"/> before running this.
		/// </summary>
		/// <param name="originalMessage">The Discord message sent by the command.</param>
		public static async Task HandleMessageCommand(DiscordMessage originalMessage) {
			DiscordUser author = originalMessage.Author;
			BotContext commandContext = BotContextRegistry.GetContext(originalMessage.Channel.Guild);
			XanBotLogger.WriteDebugLine("Executing command in bot context. Info:");
			XanBotLogger.WriteLine(commandContext.ToConsoleString(), isDebugModeOnly: true);

			XanBotMember member = XanBotMember.GetMemberFromUser(commandContext, author);
			string text = originalMessage.Content;
			if (text.ToLower().StartsWith(CommandPrefix.ToLower())) {
				text = text.Substring(CommandPrefix.Length);
			}
			while (AllowSpacesAfterPrefix && text.StartsWith(" ")) {
				text = text.Substring(1);
			}
			string[] allArgs = ArgumentSplitter.SplitArgs(text);
			string command = allArgs[0];
			string[] args = new string[0];
			if (allArgs.Length > 1) {
				args = allArgs.Skip(1).ToArray();
			}

			// Catch case: Prevent excessive command length.
			if (command.Length > 32) {
				await ResponseUtil.RespondToAsync(originalMessage, "The command you input is too long.");
				XanBotLogger.WriteLine(string.Format("§aUser \"§6{0}§a\" issued a command that was considered too long to parse.", member.FullName));
				return;
			}
			// Catch case: Strip color formatting
			command.Replace(XanBotLogger.COLOR_CODE_SYM.ToString(), "");

			XanBotLogger.WriteDebugLine("Searching for command...");
			Command execCommand = null;
			string cmdLower = command.ToLower();
			// Search the context FIRST. That ensures that context commands can override stock commands.
			foreach (Command cmd in commandContext.Commands) {
				if (cmd.Name.ToLower() == cmdLower) {
					execCommand = cmd;
					XanBotLogger.WriteDebugLine($"Found command [{cmd.Name}] in context.");
					break;
				} else {
					if (cmd.AlternateNames != null) {
						foreach (string altName in cmd.AlternateNames) {
							if (altName.ToLower() == cmdLower) {
								execCommand = cmd;
								break;
							}
						}
					}
				}
			}

			if (execCommand == null) {
				foreach (Command cmd in Commands) {
					if (cmd.Name.ToLower() == command.ToLower()) {
						execCommand = cmd;
						XanBotLogger.WriteDebugLine($"Found command [{cmd.Name}] globally.");
						break;
					} else {
						if (cmd.AlternateNames != null) {
							foreach (string altName in cmd.AlternateNames) {
								if (altName.ToLower() == cmdLower) {
									execCommand = cmd;
									break;
								}
							}
						}
					}
				}
			}


			if (execCommand != null) {
				UsagePermissionPacket usagePerms = execCommand.CanUseCommand(member);
				if (usagePerms.CanUse) {
					if (execCommand.CanUseCommandInThisChannel(member, originalMessage.Channel, out DiscordChannel optimalTargetChannel)) {
						try {
							string allArgsText = "";
							if (args.Length > 0) {
								allArgsText = text.Substring(command.Length + 1);
							}
							originalMessage.Channel.TriggerTypingAsync().GetAwaiter().GetResult();
							await execCommand.ExecuteCommandAsync(commandContext, member, originalMessage, args, allArgsText);
							XanBotLogger.WriteLine(string.Format("§aUser \"§6{0}§a\" issued command \"§6{1}§a\" with args {2}", member.FullName, command, ArrayToText(args)));
						} catch (CommandException commandErr) {
							string message = string.Format("§cFailed to issue command `{0}`: §1{1}", commandErr.Command.Name, commandErr.Message);
							await ResponseUtil.RespondToAsync(originalMessage, message);

							XanBotLogger.WriteLine(string.Format("§aUser \"§6{0}§a\" attempted to issue command \"§6{1}§a\" but it failed. The command gave the reason: §2{2}", member.FullName, commandErr.Command.Name, commandErr.Message));
						} catch (ArchonCommandException commandErr) {
							string message = string.Format("§cFailed to issue Archon Command `{0}`: §1{1}", commandErr.Command.Name, commandErr.Message);
							await ResponseUtil.RespondToAsync(originalMessage, message);

							XanBotLogger.WriteLine(string.Format("§aUser \"§6{0}§a\" attempted to issue Archon Command \"§6{1}§a\" but it failed. The command gave the reason: §2{2}", member.FullName, commandErr.Command.Name, commandErr.Message));
						} catch (TaskCanceledException taskCancel) {
							XanBotLogger.WriteException(taskCancel);
						}
					} else {
						string message = string.Format($"You can't use this command here. Go to <#{optimalTargetChannel.Id}> instead.");
						await ResponseUtil.RespondToAsync(originalMessage, message);
					}
				} else {
					//string message = string.Format("You are not authorized to use `{0}`. It is only available to permission level `{1}` and above (You are at `{2}`)", execCommand.Name, execCommand.RequiredPermissionLevel, member.PermissionLevel);
					await ResponseUtil.RespondToAsync(originalMessage, usagePerms.ErrorMessage);
					XanBotLogger.WriteLine(string.Format("§aUser \"§6{0}§a\" attempted to issue command \"§6{1}§a\" but it failed because they don't have a high enough permission level or because the command's CanUseCommand method returned false.", member.FullName, execCommand.Name));
				}
			} else {
				await ResponseUtil.RespondToAsync(originalMessage, "The command `" + command + "` does not exist.");
				XanBotLogger.WriteLine(string.Format("§aUser \"§6{0}§a\" attempted to issue command \"§6{1}§a\" but it failed because it doesn't exist.", member.FullName, command));
			}
		}

		/// <summary>
		/// Runs the <see cref="PassiveHandler"/>s for the server that <paramref name="originalMessage"/> was sent in based on the content from the message.
		/// </summary>
		/// <param name="originalMessage">The chat message.</param>
		public static async Task RunPassiveHandlersForMessage(DiscordMessage originalMessage) {
			BotContext targetContext = BotContextRegistry.GetContext(originalMessage.Channel.Guild);
			XanBotMember sender = XanBotMember.GetMemberFromUser(targetContext, originalMessage.Author);

			if (!targetContext.Virtual) {
				foreach (PassiveHandler handler in targetContext.Handlers) {
					XanBotLogger.WriteDebugLine(handler.Name);
					if (await handler.RunHandlerAsync(targetContext, sender, originalMessage)) {
						XanBotLogger.WriteDebugLine("... returned true");
						return;
					}
					XanBotLogger.WriteDebugLine("... returned false");
				}
			} else {
				XanBotLogger.WriteDebugLine("Target context is virtual. Skipping passive handlers.");
			}
		}

		/// <summary>
		/// Returns whether or not the text is formatted as a command.
		/// </summary>
		/// <param name="text">The message text.</param>
		/// <returns>Whether or not the text is formatted as a command.</returns>
		public static bool IsCommand(string text) {
			if (text.Length <= CommandPrefix.Length) return false;
			if (!text.ToLower().StartsWith(CommandPrefix.ToLower())) return false;
			return true;
		}


		/// <summary>
		/// Converts a string array into a list separated by commas with a specific format. Used for logging command usage in the console.
		/// </summary>
		/// <param name="array">The string array</param>
		/// <returns></returns>
		private static string ArrayToText(IEnumerable<string> array) {
			string retn = "[§2";
			bool hadSomething = false;
			foreach (string str in array) {
				hadSomething = true;
				if (str != array.Last()) {
					retn += str + "§a, §2";
				} else {
					retn += str + "§a]";
				}
			}
			if (!hadSomething) retn += "§a]";
			return retn;
		}
	}
}
