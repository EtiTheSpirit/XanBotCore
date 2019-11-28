﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using XanBotCore.Exceptions;
using XanBotCore.ServerRepresentation;
using XanBotCore.UserObjects;
using XanBotCore.Utility;

namespace XanBotCore.CommandSystem.Commands.ArchonCommands {
	public class ArchonCommandCurrentContext : ArchonCommand {
		public override string Name => "currentcontext";

		public override string Description => "Returns information on the BotContext representing this server.";

		public override string Syntax => Name;

		public override void ExecuteCommand(BotContext context, XanBotMember executingMember, DiscordMessage originalMessage, string[] args, string allArgs) {
			if (context == null) throw new ArchonCommandException(this, "Cannot use currentcontext from the console, as it requires an instance of BotContext to be present.");
			//ResponseUtil.RespondTo(originalMessage, context.ToStringForDiscordMessage());
			//originalMessage?.RespondAsync(embed: context.ToEmbed());
			ResponseUtil.RespondTo(originalMessage, context);
		}
	}
}
