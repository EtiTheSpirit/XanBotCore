using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XanBotCore.Exceptions;
using XanBotCore.Permissions;
using XanBotCore.ServerRepresentation;
using XanBotCore.UserObjects;
using XanBotCore.Utility;

namespace XanBotCore.CommandSystem.Commands {
	class CommandHelp : Command {

		private const int SPACES_BETWEEN_NAME_AND_PERMS = 34;

		public override string Name { get; } = "help";

		public override string Description { get; } = "Lists every command or returns information on a command.\n\nSome commands may show something called \"Arguments\" as part of their documentation. This is the text like `<someArg>` or `[someArg]`.\n" +
			"If you need help understanding what these mean, please refer to <https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/command-line-syntax-key>\n**NOTE:** The table brackets (shift + []) have been replaced with colons (`:`) in this bot's formatting.";

		public override string Syntax => Name + " [commandName]";

		public override byte RequiredPermissionLevel { get; } = PermissionRegistry.PERMISSION_LEVEL_STANDARD_USER;

		public override async Task ExecuteCommandAsync(BotContext context, XanBotMember executingMember, DiscordMessage originalMessage, string[] args, string allArgs) {
			if (args.Length == 0) {
				string text = "Commands in yellow with a `+` before them are commands you can use. Commands in red with a `-` before them are commands you cannot use. ";
				text += "\nSay **`{0}help command_name_here`** to get more documentation on a specific command. Say **`{0}help help`** to get information on how commands work.";
				text += "```diff\n";

				foreach (Command cmd in CommandMarshaller.Commands) {
					int spaces = SPACES_BETWEEN_NAME_AND_PERMS;
					string usagePrefix = "+ ";
					bool canUse = true;
					if (executingMember != null) {
						canUse = cmd.CanUseCommand(executingMember).CanUse;
						usagePrefix = canUse ? "+ " : "- ";
					}
					string commandInfo = usagePrefix + cmd.Name;
					text += commandInfo;

					if (!canUse) {
						spaces -= commandInfo.Length;
						for (int i = 0; i < spaces; i++) {
							text += " ";
						}
						text += $"Requires Permission Level {cmd.RequiredPermissionLevel} (or higher).";
					}
					text += "\n";
				}

				if (context != null && context.Commands.Length > 0) {
					text += "\nCommands specific to this server:\n\n";
					foreach (Command cmd in context.Commands) {
						// Catch case: If this command overrides stock behavior, we don't need to show it because that'd show two commands in the list.
						if (!IsCommandInArray(cmd, CommandMarshaller.Commands)) {
							// In this case it's a unique command so we can write it.
							int spaces = SPACES_BETWEEN_NAME_AND_PERMS;
							string usagePrefix = "+";
							bool canUse = true;
							if (executingMember != null) {
								canUse = cmd.CanUseCommand(executingMember).CanUse;
								usagePrefix = canUse ? "+ " : "- ";
							}
							string commandInfo = usagePrefix + cmd.Name;
							text += commandInfo;
							if (!canUse) {
								spaces -= commandInfo.Length;
								for (int i = 0; i < spaces; i++) {
									text += " ";
								}
								text += $"Requires Permission Level {cmd.RequiredPermissionLevel} (or higher).";
							}
							text += "\n";
						}
					}
				}

				text += "```\n";

				text = string.Format(text, CommandMarshaller.CommandPrefix);

				await ResponseUtil.RespondToAsync(originalMessage, text);
			} else if (args.Length == 1) {
				string command = args[0];
				string cmdLower = command.ToLower();
				if (context.Commands.Length > 0) {
					foreach (Command cmd in context.Commands) {
						if (cmd.Name.ToLower() == cmdLower) {
							await ResponseUtil.RespondToAsync(originalMessage, GetFormattedCommandHelpInfo(cmd, cmd.Name));
							return;
						} else if (cmd.AlternateNames != null) {
							foreach (string alt in cmd.AlternateNames) {
								if (alt.ToLower() == cmdLower) {
									await ResponseUtil.RespondToAsync(originalMessage, GetFormattedCommandHelpInfo(cmd, alt));
									return;
								}
							}
						}
					}
				}
				foreach (Command cmd in CommandMarshaller.Commands) {
					if (cmd.Name.ToLower() == cmdLower) {
						await ResponseUtil.RespondToAsync(originalMessage, GetFormattedCommandHelpInfo(cmd, cmd.Name));
						return;
					} else if (cmd.AlternateNames != null) {
						foreach (string alt in cmd.AlternateNames) {
							if (alt.ToLower() == cmdLower) {
								await ResponseUtil.RespondToAsync(originalMessage, GetFormattedCommandHelpInfo(cmd, alt));
								return;
							}
						}
					}
				}
				throw new CommandException(this, "Command `" + command + "` does not exist.");
			} else {
				throw new CommandException(this, "Invalid argument count. Expected no arguments, or one argument which is the name of the command you wish to get details on.");
			}
		}

		public string GetFormattedCommandHelpInfo(Command cmd, string indexedBy) {
			int locatedGraves = 0;
			foreach (char c in cmd.Syntax.ToCharArray()) {
				if (c == '`') locatedGraves++;
			}

			string text = string.Format(
				"**Command:** `{0}` \n{1}\n\n**Usage:** `{2}",
				cmd.Name,
				string.Format(cmd.Description, CommandMarshaller.CommandPrefix),
				string.Format(cmd.Syntax, CommandMarshaller.CommandPrefix)
			);
			if (locatedGraves % 2 == 0) {
				// Effectively what this does is prevents formatting issues from if users specify custom code formatting.
				// They are expected to terminate the syntax code block manually in syntax.
				// If they use an odd number of graves, it means that they have overridden it and we need to not include a trailing grave since that will show up.
				text += '`';
			}

			string alsoIndexedBy = null;
			if (cmd.AlternateNames != null) {
				if (indexedBy.ToLower() == cmd.Name.ToLower()) {
					alsoIndexedBy = StrArrayToFormatedList(cmd.AlternateNames);
				} else {
					alsoIndexedBy = "`" + cmd.Name + "`";
					if (cmd.AlternateNames.Length > 1) {
						alsoIndexedBy += ", "; // If it's 1, then that means there's no other names to show so it's just gonna be the command's name.
						foreach (string alt in cmd.AlternateNames) {
							if (alt.ToLower() != indexedBy.ToLower()) {
								alsoIndexedBy += "`" + alt + "`";
								if (alt != cmd.AlternateNames.Last()) {
									alsoIndexedBy += ", ";
								}
							}
						}
					}
				}
			}

			if (alsoIndexedBy != null) {
				text += "\n**Can also be run with:** " + alsoIndexedBy;
			}

			return text;
		}

		private static string StrArrayToFormatedList(string[] list) {
			string o = "";
			foreach (string s in list) {
				o += "`" + s + "`";
				if (s != list.Last()) {
					o += ", ";
				}
			}
			return o;
		}

		private static bool IsCommandInArray(Command cmd, Command[] arr) {
			foreach (Command c in arr) {
				if (c.Name.ToLower() == cmd.Name.ToLower()) {
					return true;
				}
			}
			return false;
		}
	}
}
