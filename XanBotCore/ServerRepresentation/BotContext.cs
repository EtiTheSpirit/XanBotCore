using DSharpPlus.Entities;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using XanBotCore.CommandSystem;
using XanBotCore.PassiveHandlers;
using XanBotCore.Logging;
using XanBotCore.Utility.DiscordObjects;

namespace XanBotCore.ServerRepresentation {

	/// <summary>
	/// Represents a "Bot Context" which describes a bot's presence in a server. Classes that implement this interface can be created to give unique functionality to servers (e.g. commands and passive handlers).<para/>
	/// If a server does not have a context designed specifically for it, a virtual context will be created.<para/>
	/// See <see cref="BotContextRegistry"/> for how to get contexts for a server. User-defined contexts will automatically be registered.
	/// </summary>
	public abstract class BotContext : IConsolePrintable, IEmbeddable, IEquatable<BotContext> {

		protected BotContext() {
			if (!Virtual) BotContextRegistry.UserDefinedContextsInternal.Add(this);
		}

		/// <summary>
		/// The display name of this context. If this is a virtual context, it will be the server's name.
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// A filesystem-friendly name for this bot context. This should not contain any characters that are not allowed in a Windows file system. If this is a virtual context, it will be VirtualContext-SERVER_ID_HERE
		/// </summary>
		public abstract string DataPersistenceName { get; }

		/// <summary>
		/// The ID of the server this context represents.
		/// </summary>
		public abstract ulong ServerId { get; }


		/// <summary>
		/// Represents whether or not this context is virtual internally. Reference <see cref="Virtual"/> instead and do not manipulate this value.
		/// </summary>
		internal virtual bool IsVirtualInternal => false;

		/// <summary>
		/// Will be true if this context is a <see cref="VirtualBotContext"/>, and false if it is not.
		/// </summary>
		public bool Virtual => IsVirtualInternal;


		private DiscordGuild CachedServer = null;
		/// <summary>
		/// A reference to the underlying server of this bot context.
		/// </summary>
		public DiscordGuild Server {
			get {
				if (CachedServer == null) CachedServer = XanBotCoreSystem.Client.GetGuildAsync(ServerId).GetAwaiter().GetResult();
				return CachedServer;
			}
		}

		private DiscordMember CachedBot = null;

		/// <summary>
		/// Returns the <see cref="DiscordMember"/> representing the bot in this <see cref="Server"/>.
		/// </summary>
		public DiscordMember Bot {
			get {
				if (CachedBot == null) CachedBot = Server.GetMemberAsync(XanBotCoreSystem.Client.CurrentUser.Id).GetAwaiter().GetResult();
				return CachedBot;
			}
		}

		/// <summary>Stores whether or not the bot has a registered role, since the bot will not have roles if it doesn't have special permissions.</summary>
		private bool DoesBotHaveRole = true;
		private DiscordRole CachedRole = null;

		/// <summary>
		/// Returns the integrated <see cref="DiscordRole"/> associated with this bot. This will be null if the bot does not have an integrated role with custom permissions.
		/// </summary>
		public DiscordRole BotRole {
			get {
				if (CachedRole == null && DoesBotHaveRole) {
					XanBotLogger.WriteDebugLine("Bot role does not exist yet. Going through the bot's roles...");
					foreach (DiscordRole role in Bot.Roles) {
						if (role.IsManaged) {
							// Managed (integrated) roles cannot be granted, so this bot will only have one managed role, which is its own.
							XanBotLogger.WriteDebugLine("Found bot role.");
							CachedRole = role;
							break;
						}
					}
					if (CachedRole == null) {
						// Still found nothing. There must be no role.
						XanBotLogger.WriteDebugLine("No bot role was found.");
						DoesBotHaveRole = false;
					}
					
				}
				return CachedRole;
			}
		}

		protected virtual Command[] ContextSpecificCommands { get; } = new Command[0];
		protected virtual PassiveHandler[] ContextSpecificHandlers { get; } = new PassiveHandler[0];
		private Command[] CommandCache = null;
		private PassiveHandler[] HandlerCache = null;

		/// <summary>
		/// The commands that are specific to this context. If this is a virtual context, this array will be empty. The result of this array is cached.
		/// </summary>
		public Command[] Commands {
			get {
				if (CommandCache == null) {
					CommandCache = ContextSpecificCommands;
					if (CommandCache == null) CommandCache = new Command[0];
				}
				return CommandCache;
			}
		}


		/// <summary>
		/// The passive handlers that are specific to this context. If this is a virtual context, this array will be empty. The result of this array is cached.
		/// </summary>
		public PassiveHandler[] Handlers {
			get {
				if (HandlerCache == null) {
					HandlerCache = ContextSpecificHandlers;
					if (HandlerCache == null) HandlerCache = new PassiveHandler[0];
				}
				return HandlerCache;
			}
		}

		/// <summary>
		/// The default permission level of users in this server.
		/// </summary>
		public virtual Dictionary<ulong, byte> DefaultUserPermissions { get; } = new Dictionary<ulong, byte>();

		/// <summary>
		/// Defines any code that needs to run when initializing this context.
		/// </summary>
		public virtual void AfterContextInitialization() { }

		/// <summary>
		/// Convert this <see cref="BotContext"/> to a string.
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			string ret = $"=> Context Name: {Name}\n"
				+ $"=> Data Persistence Name: {DataPersistenceName}\n"
				+ $"=> Target Server: {ServerId} ({Server.Name})\n";
			if (BotRole != null) {
				ret += $"=> Bot Role: {BotRole?.Id} ({BotRole?.Name})\n";
			} else {
				ret += "=> Bot Role: N/A\n";
			}
			ret += $"=> Is Virtual Context: {Virtual}";
			return ret;
		}

		/// <summary>
		/// A variation of <see cref="ToString"/> that is formatted for sending as a <see cref="DiscordMessage"/>.
		/// </summary>
		/// <param name="isForDiscordMessage"></param>
		/// <returns></returns>
		[Obsolete("Consider using ToEmbed instead.")]
		public string ToStringForDiscordMessage() {
			string ret = "**__BotContext Information__**\n"
				+ $"**Context Name:** `{Name}`\n"
				+ $"**Data Persistence Name:** `{DataPersistenceName}`"
				+ $"**Target Server:** `{ServerId}` ({Server.Name})\n";
			if (BotRole != null) {
				ret += $"**Bot Role:** `{BotRole?.Id}` ({BotRole?.Name})\n";
			} else {
				ret += "**Bot Role:** N/A\n";
			}
			ret += $"**Is Virtual Context:** `{Virtual}`";
			return ret;
		}

		public virtual string ToConsoleString() {
			string ret = "§2BotContext Information\n"
				+ $"§2> Context Name: §a{Name}§2\n"
				+ $"> Data Persistence Name: §a{DataPersistenceName}§2\n"
				+ $"> Target Server: §a{ServerId} §2(§6{Server.Name}§2)\n";
			if (BotRole != null) {
				ret += $"> Bot Role: §a{BotRole.Id} §2(§6{BotRole.Name}§2)\n";
			} else {
				ret += "> Bot Role: §aN/A§2\n";
			}

			ret += $"> Is Virtual Context: §a{Virtual}";
			return ret;
		}

		public virtual DiscordEmbed ToEmbed() {
			DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
			builder.Title = "**BotContext Information Dump**";

			string baseInfo = $"**Context Display Name:** {Name}\n**Data Persistence Name:** {DataPersistenceName}\n**Is Virtual:** {Virtual}";
			builder.AddField("Context Object Information", baseInfo);

			string serverInfo = $"**Target Server:** {Server.Name} ({ServerId})\n";
			if (BotRole != null) {
				serverInfo += $"**Integrated Role:** {BotRole.Name} ({BotRole.Id})";
			} else {
				serverInfo += "**Integrated Role:** N/A";
			}
			builder.AddField("Context Target Information", serverInfo);

			builder.Color = new DiscordColor(0, 255, 127);

			return builder.Build();
		}

		public static bool operator ==(BotContext alpha, BotContext bravo) {
			bool alphaValid = alpha is object;
			bool bravoValid = bravo is object;
			if (alphaValid == bravoValid) {
				if (alphaValid == false) return true;
				return alpha.Equals(bravo);
			}
			return false;
		}

		public static bool operator !=(BotContext alpha, BotContext bravo) => !(alpha == bravo);

		public override bool Equals(object obj) => obj is BotContext context ? Equals(context) : false;

		public override int GetHashCode() => HashCode.Combine(ServerId);

		public bool Equals(BotContext other) {
			if (ReferenceEquals(other, this)) return true;
			return ServerId == other.ServerId;
		}

		
	}

	/// <summary>
	/// Stores all custom contexts, custom and virtual. Initialize custom bot contexts and store them in <see cref="UserDefinedContexts"/> on bot initialization.
	/// </summary>
	public class BotContextRegistry {

		internal static List<BotContext> UserDefinedContextsInternal = new List<BotContext>();
		/// <summary>
		/// All user-defined <see cref="BotContext"/>s.
		/// </summary>
		public static IReadOnlyList<BotContext> UserDefinedContexts => UserDefinedContextsInternal.AsReadOnly();

		/// <summary>
		/// The <see cref="VirtualBotContext"/> created on the fly by the bot.
		/// </summary>
		private static readonly List<VirtualBotContext> VirtualContexts = new List<VirtualBotContext>();
		
		/// <summary>
		/// Every single <see cref="BotContext"/> (including virtual) that exists in memory.
		/// </summary>
		public static IReadOnlyList<BotContext> AllContexts {
			get {
				List<BotContext> retn = new List<BotContext>();
				retn.AddRange(UserDefinedContextsInternal);
				retn.AddRange(VirtualContexts);
				return retn.AsReadOnly();
			}
		}

		/// <summary>
		/// Gets a bot context from the specified server. This creates and registers a new <see cref="VirtualBotContext"/> if the server does not already have its own context.
		/// </summary>
		/// <param name="server">The server that the requested bot context uses.</param>
		/// <returns></returns>
		public static BotContext GetContext(DiscordGuild server) {
			XanBotLogger.WriteDebugLine("Searching for user-defined contexts...");
			foreach (BotContext ctx in UserDefinedContexts) {
				if (ctx.Server == server) {
					XanBotLogger.WriteDebugLine("Found user-defined context! Returning.");
					return ctx;
				}
			}

			// It's not a custom context. Did we already create a virtual context?
			XanBotLogger.WriteDebugLine("No user-defined contexts found. Searching for pre-existing virtual contexts...");
			foreach (VirtualBotContext ctx in VirtualContexts) {
				if (ctx.Server == server) {
					XanBotLogger.WriteDebugLine("Pre-existing virtual context found! Returning.");
					return ctx;
				}
			}

			// Nope! Create a new virtual context for this server.
			XanBotLogger.WriteDebugLine("No virtual contexts found. Creating new virtual context for this server...");
			VirtualBotContext serverCtx = new VirtualBotContext(server);
			VirtualContexts.Add(serverCtx);
			return serverCtx;
		}

		public static T GetUserDefinedContext<T>() where T : BotContext {
			XanBotLogger.WriteDebugLine("Searching for user-defined context by type...");
			foreach (BotContext ctx in UserDefinedContexts) {
				if (typeof(T) == ctx.GetType()) {
					XanBotLogger.WriteDebugLine("Found context.");
					return (T)ctx;
				}
			}
			XanBotLogger.WriteDebugLine("Failed to find context.");
			return null;
		}

		/// <summary>
		/// Should only be called by the initializer. This iterates through all active assemblies, finds classes that extend <see cref="BotContext"/> (and NOT <see cref="VirtualBotContext"/>), and instantiates them so that they auto-register.
		/// </summary>
		internal static void InitializeAllContexts() {
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

			foreach (Assembly asm in assemblies) {
				XanBotLogger.WriteDebugLine("Searching assembly [" + asm.GetName().Name + "] for BotContext instances...");
				foreach (Type type in asm.GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(BotContext)) && myType != typeof(VirtualBotContext) && !myType.IsSubclassOf(typeof(VirtualBotContext)))) {
					XanBotLogger.WriteDebugLine("§8Found BotContext [" + type.Name + "]. Instantiating...");
					BotContext ctx = (BotContext)Activator.CreateInstance(type);
					ctx.AfterContextInitialization();
				}
			}
		}

	}
}
