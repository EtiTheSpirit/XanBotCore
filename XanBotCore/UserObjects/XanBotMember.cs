#pragma warning disable CS1998
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XanBotCore.Logging;
using XanBotCore.Permissions;
using XanBotCore.ServerRepresentation;
using XanBotCore.Utility;
using XanBotCore.Utility.DiscordObjects;

namespace XanBotCore.UserObjects {

	/// <summary>
	/// Represents a wrapped member object that offers some extra data such as permission level and the <see cref="BotContext"/> that this member exists in.
	/// </summary>
	public class XanBotMember : IEmbeddable, IEquatable<XanBotMember> {
		/// <summary>
		/// A cache storing XanBotMembers referenced by BotContext, then by user ID for usage in the get function.
		/// </summary>
		private static readonly Dictionary<BotContext, Dictionary<ulong, XanBotMember>> XBMCache = new Dictionary<BotContext, Dictionary<ulong, XanBotMember>>();

		#region Properties
		/// <summary>
		/// The underlying DiscordUser of this XanBotMember.
		/// </summary>
		public DiscordUser BaseUser { get; }

		/// <summary>
		/// The bot context that this member lives in.
		/// </summary>
		public BotContext Context { get;}

		/// <summary>
		/// The underlying DiscordMember of this XanBotMember.
		/// </summary>
		public DiscordMember Member {
			// This needs to be acquired upon every reference. This is due to member updates.
			// The utilized version of DSharpPlus handles caching on its own so I won't worry about it here.
			get {
				try {
					return Context.Server.GetMemberAsync(BaseUser.Id).GetAwaiter().GetResult();
				} catch (NotFoundException) {
					return null;
				}
			}
		}

		private byte PermissionLevelInternal = PermissionRegistry.DefaultPermissionLevel;

		/// <summary>
		/// This user's registered permission level, which determines accessible commands.
		/// </summary>
		public byte PermissionLevel {
			get {
				if (BaseUser == XanBotCoreSystem.Client.CurrentUser) return 255;
				if (Member == null) return 0;
				return PermissionLevelInternal;
			}
			set {
				if (BaseUser == XanBotCoreSystem.Client.CurrentUser) return; // Stop if we're modifying the bot
				if (PermissionLevelInternal == value) return; //Stop if it's the same
				XanBotLogger.WriteLine("§aPermission Level of user \"§6" + FullName + "\"§a changed from §e" + PermissionLevelInternal + "§a to §e" + value + "§a in context §6" + Context.Name);
				PermissionLevelInternal = value;

				// It is IMPERATIVE that this is after PermissionLevelInternal = value (because the method references this property)
				PermissionRegistry.UpdatePermissionLevelOfMember(this, true);
			}
		}

		/// <summary>
		/// This user's username#discriminator e.g. Xan the Dragon#1760
		/// </summary>
		public string FullName {
			get {
				return Username + "#" + Discriminator;
			}
		}

		/// <summary>
		/// This user's username.
		/// </summary>
		public string Username => BaseUser.Username;

		/// <summary>
		/// This user's discriminator, not including the #.
		/// </summary>
		public string Discriminator => BaseUser.Discriminator;

		/// <summary>
		/// A reference to the underlying <see cref="DiscordMember.Nickname"/> property.
		/// </summary>
		public string Nickname => Member.Nickname;

		/// <summary>
		/// Returns <see cref="Nickname"/> if it is not empty nor null, and <see cref="Username"/> if it is.
		/// </summary>
		public string DisplayName {
			get {
				if (Nickname == default || Nickname == null || Nickname.Length == 0) {
					return Username;
				}
				return Nickname;
			}
		}

		/// <summary>
		/// A reference to <see cref="BaseUser"/>'s ID.
		/// </summary>
		public ulong Id => BaseUser.Id;

		/// <summary>
		/// A reference to <see cref="BaseUser"/>'s Mention property.
		/// </summary>
		public string Mention => BaseUser.Mention;

		/// <summary>
		/// A more reliable list of <see cref="DiscordRole"/>s that this user has. "Reliablility" references how this data is populated and cached.<para/>
		/// DSharpPlus's built in caches can sometimes be faulty and not contain all of the member's roles, especialy after changes made to their roles.<para/>
		/// This list, on the other hand, is always kept in sync as closely as possible by listening to all role change events.
		/// </summary>
		public IReadOnlyList<DiscordRole> Roles => RolesInternal.AsReadOnly();
		private List<DiscordRole> RolesInternal { get; set; }

		#endregion

		#region Static Event Control Systems

		private static bool EventsConnected = false;

		private static void SetupEventSystemsIfNeeded() {
			if (EventsConnected) return;
			EventsConnected = true;

			XanBotCoreSystem.Client.GuildMemberUpdated += async evt => {
				UpdateMemberRoleData(evt.Member, evt.RolesAfter.ToList());
			};
			
		}

		private static void UpdateMemberRoleData(DiscordMember target, List<DiscordRole> roles) {
			XanBotMember targetMember = GetMemberFromDiscordMember(target);
			targetMember.RolesInternal = roles;
		}

		#endregion

		#region CtorMethods
		/// <summary>
		/// Create a new XanBotMember from a DiscordUser. In standard cases this function would be impossible without a server reference, but this reference exists in the bot since it targets one server.
		/// </summary>
		/// <param name="user">The DiscordUser to use as the underlying user.</param>
		private XanBotMember(BotContext context, DiscordUser user) {
			try {
				SetupEventSystemsIfNeeded();

				BaseUser = user;
				Context = context;
				RolesInternal = Member.Roles.ToList();
				PermissionLevelInternal = PermissionRegistry.GetPermissionLevelOfUser(user.Id, context);
			}
			catch (Exception ex) {
				XanBotLogger.WriteException(ex);
			}
		}

		/// <summary>
		/// Create a <see cref="XanBotMember"/> from a <seealso cref="DiscordUser"/> and a <seealso cref="DiscordGuild"/>
		/// </summary>
		/// <param name="server">The server that this <see cref="XanBotMember"/> exists in.</param>
		/// <param name="user">The <seealso cref="DiscordUser"/> to create the member from.</param>
		/// <returns></returns>
		public static XanBotMember GetMemberFromUser(DiscordGuild server, DiscordUser user) {
			if (user == null) return null;
			BotContext ctxForServer = BotContextRegistry.GetContext(server);
			return GetMemberFromUser(ctxForServer, user);
		}

		/// <summary>
		/// Create a <see cref="XanBotMember"/> from a <seealso cref="DiscordUser"/> and a <seealso cref="BotContext"/>
		/// </summary>
		/// <param name="context">The context that this <see cref="XanBotMember"/> exists in.</param>
		/// <param name="user">The <seealso cref="DiscordUser"/> to create the member from.</param>
		/// <returns></returns>
		public static XanBotMember GetMemberFromUser(BotContext context, DiscordUser user) {
			if (user == null) return null;
			if (XBMCache.TryGetValue(context, out Dictionary<ulong, XanBotMember> registry)) {
				if (registry.TryGetValue(user.Id, out XanBotMember result)) {
					return result;
				}
			} else {
				XBMCache[context] = new Dictionary<ulong, XanBotMember>();
			}
			XanBotMember member = new XanBotMember(context, user);
			XBMCache[context][member.Id] = member;
			return member;
		}

		/// <summary>
		/// Create a <see cref="XanBotMember"/> from a <see cref="DiscordMember"/>
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public static XanBotMember GetMemberFromDiscordMember(DiscordMember member) {
			return GetMemberFromUser(member.Guild, member);
		}

		/// <summary>
		/// Returns a XanBotMember created from the data in a <seealso cref="ShallowXanBotMember"/>.
		/// </summary>
		/// <param name="shallow">The <seealso cref="ShallowXanBotMember"/> reference to create the member from.</param>
		/// <returns></returns>
		// This might be removed.
		public static XanBotMember GetMemberFromShallow(ShallowXanBotMember shallow) {
			try {
				DiscordUser user = XanBotCoreSystem.Client.GetUserAsync(shallow.UserId).GetAwaiter().GetResult();
				DiscordGuild server = XanBotCoreSystem.Client.GetGuildAsync(shallow.ServerId).GetAwaiter().GetResult();
				return GetMemberFromUser(server, user);
			} catch (Exception) {
				return null;
			}
		}
		#endregion

		#region Member Control

		/// <summary>
		/// Grant the specified <see cref="DiscordRole"/> to this member.
		/// </summary>
		/// <param name="role">The <see cref="DiscordRole"/> to give</param>
		/// <param name="reason">The reason to provide in the audit log</param>
		/// <exception cref="ArgumentNullException">If the role is null.</exception>
		/// <exception cref="InvalidOperationException">If the role is integrated and cannot be granted or taken away from users.</exception>
		public async Task GrantRoleAsync(DiscordRole role, string reason = null) {
			if (role == null) throw new ArgumentNullException("role");
			if (role.IsManaged) throw new InvalidOperationException("Cannot control managed roles.");
			await Member.GrantRoleAsync(role, reason);
		}

		/// <summary>
		/// Remove the specified <see cref="DiscordRole"/> from this member.
		/// </summary>
		/// <param name="role">The <see cref="DiscordRole"/> to take</param>
		/// <param name="reason">The reason to provide in the audit log</param>
		/// <exception cref="ArgumentNullException">If the role is null.</exception>
		/// <exception cref="InvalidOperationException">If the role is integrated and cannot be granted or taken away from users.</exception>
		public async Task RevokeRoleAsync(DiscordRole role, string reason = null) {
			if (role == null) throw new ArgumentNullException("role");
			if (role.IsManaged) throw new InvalidOperationException("Cannot control managed roles.");
			await Member.RevokeRoleAsync(role, reason);
		}

		/// <summary>
		/// Replaces this member's roles so that they only have the roles given in this array.<para/>
		/// This provides protections against integrated roles (e.g. Nitro Booster) so that the replacement operation may still function, politely ignoring the roles it cannot manage.
		/// </summary>
		/// <param name="roles"></param>
		/// <param name="reason"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException">If the roles list is null.</exception>
		public async Task ReplaceRolesAsync(IEnumerable<DiscordRole> roles, string reason = null) {
			if (roles == null) throw new ArgumentNullException("roles");

			List<DiscordRole> rolesAsList = roles.ToList();
			// Does the list of new roles contain integrated roles?
			foreach (DiscordRole role in rolesAsList) {
				if (role.IsManaged && !Roles.Contains(role)) {
					// The role is managed and we don't already have it.
					// Remove it!
					rolesAsList.Remove(role);
				}
			}

			// Likewise, does the user have integrated roles we might be accidentally removing?
			foreach (DiscordRole role in Roles) {
				if (role.IsManaged) {
					// The role is managed, so we need to ensure it's in the role list. Add it if it's not there already.
					if (!rolesAsList.Contains(role)) rolesAsList.Add(role);
				}
			}

			await Member.ReplaceRolesAsync(rolesAsList.ToArray(), reason);
		}

		/// <summary>
		/// If the user does not have the specified <see cref="DiscordRole"/>, it will give it to them. Likewise, if the user DOES have the specified <see cref="DiscordRole"/>, it will take it from them.<para/>
		/// Returns true if the user was given the role, and false if the role was taken away from the user.
		/// </summary>
		/// <param name="role">The <see cref="DiscordRole"/> to give or take</param>
		/// <param name="reason">The reason to provide in the audit log</param>
		/// <exception cref="ArgumentNullException">If the role is null.</exception>
		/// <exception cref="InvalidOperationException">If the role is integrated and cannot be granted or taken away from users.</exception>
		public async Task<bool> ToggleRoleAsync(DiscordRole role, string reason = null) {
			if (role == null) throw new ArgumentNullException("role");
			if (role.IsManaged) throw new InvalidOperationException("Cannot control managed roles.");
			if (HasRole(role)) {
				await RevokeRoleAsync(role, reason);
				return false;
			} else {
				await GrantRoleAsync(role, reason);
				return true;
			}
		}

		/// <summary>
		/// Returns whether or not this member has the specified <see cref="DiscordRole"/>
		/// </summary>
		/// <param name="role">The <see cref="DiscordRole"/> to look for</param>
		/// <exception cref="ArgumentNullException"/>
		public bool HasRole(DiscordRole role) {
			if (role == null) throw new ArgumentNullException("role");
			return Roles.Contains(role);
		}

		#endregion

		#region Member Utilities

		/// <summary>
		/// DM this user.
		/// </summary>
		/// <param name="message">The string message to send.</param>
		/// <param name="embed">An optional embed to send.</param>
		/// <returns></returns>
		public async Task SendDMAsync(string message = null, DiscordEmbed embed = null) {
			DiscordChannel dm = await Member.CreateDmChannelAsync();
			await dm.SendMessageAsync(message, false, embed);
		}

		/// <summary>
		/// Calls <see cref="ToString(DisplayType)"/> with an argument of <see cref="DiscordUserExtensions.DisplayType.NicknameUserId"/>
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return ToString(DisplayType.NicknameUserId);
		}

		/// <summary>
		/// Calls the extension method provided by <see cref="DiscordUserExtensions"/>: <see cref="DiscordUserExtensions.GetFormattedUser(DiscordUser, DisplayType)"/><para/>
		/// See <see cref="DisplayType"/> for more information.
		/// </summary>
		/// <param name="displayType"></param>
		/// <returns></returns>
		public string ToString(DisplayType displayType) {
			return Member.GetFormattedMember(displayType);
		}

		public DiscordEmbed ToEmbed() {
			DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
			builder.Title = "XanBotMember Info";
			builder.Description = "Member: " + Mention;
			builder.AddField("Basic Information", $"**Display Name:** {DisplayName}\n**Username:** {Username}\n**Discriminator:** {Discriminator}\n**GUID:** {Id}");
			builder.AddField("Parent Context", Context.ToString());
			builder.AddField("Permission Level", PermissionLevel.ToString());
			return builder.Build();
		}

		#endregion

		#region Member Equality (Racism not included)

		public static bool operator ==(XanBotMember alpha, XanBotMember bravo) {
			bool alphaValid = alpha is object;
			bool bravoValid = bravo is object;
			if (alphaValid == bravoValid) {
				if (alphaValid == false) return true;
				return alpha.Equals(bravo);
			}
			return false;
		}

		public static bool operator !=(XanBotMember alpha, XanBotMember bravo) => !(alpha == bravo);

		public override bool Equals(object obj) => obj is XanBotMember member ? Equals(member) : false;

		public override int GetHashCode() => HashCode.Combine(Id, Context);
		
		public bool Equals(XanBotMember other) {
			if (ReferenceEquals(this, other)) return true;
			if (other is XanBotMember) {
				return Id == other.Id && Context == other.Context;
			}
			return false;
		}

		public static implicit operator DiscordMember(XanBotMember src) {
			return src.Member;
		}

		#endregion

	}

	/// <summary>
	/// A "Shallow" <see cref="XanBotMember"/> which represents a member solely through user ID and server ID. This does not store any other data, and is used to<para/>
	/// create a <see cref="XanBotMember"/> object without a user (almost in an abstract form, in a sense).<para/>
	/// <para/>
	/// This is mainly used for data persistence where the permission registry needs to create user objects from user ID and server ID so that data<para/>
	/// can be loaded later on by an actual member object with proper data.
	/// </summary>
	public class ShallowXanBotMember {

		public static List<ShallowXanBotMember> ShallowMembers = new List<ShallowXanBotMember>();

		public ulong UserId { get; private set; }
		public ulong ServerId { get; private set; }

		private ShallowXanBotMember(ulong userId, ulong serverId) {
			UserId = userId;
			ServerId = serverId;
		}

		public bool CompareShallowMember(ulong userId, ulong serverId) {
			return UserId == userId && ServerId == serverId;
		}

		public bool DoesShallowRepresentDeep(XanBotMember deep) {
			return UserId == deep.BaseUser.Id && ServerId == deep.Context.ServerId;
		}

		public static ShallowXanBotMember GetShallowFromDeep(XanBotMember fullMemberObject) {
			foreach (ShallowXanBotMember shallow in ShallowMembers) {
				if (shallow.DoesShallowRepresentDeep(fullMemberObject)) {
					return shallow;
				}
			}
			ShallowXanBotMember member = new ShallowXanBotMember(fullMemberObject.BaseUser.Id, fullMemberObject.Context.ServerId);
			ShallowMembers.Add(member);
			return member;
		}

		public static ShallowXanBotMember GetShallowFromRaw(ulong userId, ulong serverId) {
			foreach (ShallowXanBotMember shallow in ShallowMembers) {
				if (shallow.CompareShallowMember(userId, serverId)) {
					return shallow;
				}
			}
			ShallowXanBotMember member = new ShallowXanBotMember(userId, serverId);
			ShallowMembers.Add(member);
			return member;
		}

	}
}
