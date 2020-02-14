using DSharpPlus;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XanBotCore;
using XanBotCore.Exceptions;
using XanBotCore.Logging;
using XanBotCore.ServerRepresentation;
using XanBotCore.UserObjects;
using XanBotCore.Utility;
using XanBotCore.Utility.DiscordObjects;

namespace XanBotCore.Utility.DiscordObjects {

	/// <summary>
	/// Represents a <see cref="DiscordRole"/> that is "pseudo-integrated" in that its data is managed, and only certain members can have it.
	/// </summary>
	public class PseudoIntegratedRole {

		/// <summary>
		/// True if this is a valid instance of <see cref="PseudoIntegratedRole"/>. This will be false if the role is acquired by ID and the role for said ID doesn't exist.
		/// </summary>
		public bool Complete { get; }

		/// <summary>
		/// True if the role enforces which users are allowed to have this role. Default value is true.
		/// </summary>
		public bool EnforceMembership { get; set; } = true;

		/// <summary>
		/// The context that this role exists in.
		/// </summary>
		public BotContext Context { get; }

		/// <summary>
		/// The role that this instance controls.<para/>
		/// Internal note: This should only have its setter called if the role is deleted.
		/// </summary>
		public DiscordRole Role { get; internal set; }

		/// <summary>
		/// The template data for this role if it needs to be created.
		/// </summary>
		public RoleTemplate TemplateData { get; }

		/// <summary>
		/// A list of User IDs that represents the users who are supposed to have this role. See <see cref="MembershipDeterminationMethod"/> for whether or not this will be used.
		/// </summary>
		public List<ulong> UsersSupposedToHaveThisRole { get; }

		/// <summary>
		/// An optional function to run to check if a user can have this role. See <see cref="MembershipDeterminationMethod"/> for whether or not this will be used.
		/// </summary>
		public Func<XanBotMember, bool> ExtraUsageRequirements { get; set; } = null;

		/// <summary>
		/// The behavior used when determining if a member should have this role. This is only used if <see cref="EnforceMembership"/> is true. Its default value is <see cref="MembershipBehavior.ListAndMethod"/>
		/// </summary>
		public MembershipBehavior MembershipDeterminationMethod { get; set; } = MembershipBehavior.ListAndMethod;

		/// <summary>
		/// The properties of this <see cref="PseudoIntegratedRole"/> that are enforced and, by extension, cannot be changed by any administrators.<para/>
		/// This value is clamped to the underlying <see cref="RoleTemplate"/>'s properties. If a value in the template is null, that takes precedence over this value, and the value will never be checked nor enforced.<para/>
		/// If set to null, the null will be caught and this will be set to be equal to <see cref="TemplateData"/>'s ComparisonMethod flags instead. (As a result, this value will never be null)
		/// </summary>
		public RoleComparisonFlags? PropertiesToEnforce {
			get => _CompFlags;
			set {
				_CompFlags = value.GetValueOrDefault(TemplateData.ComparisonMethod);
			}
		}
		private RoleComparisonFlags _CompFlags;

		/// <summary>
		/// Create a new managed role from the specified role, context, and optional array of users allowed to have this role.
		/// </summary>
		/// <param name="role">The role to target.</param>
		/// <param name="context">The context this role exists in.</param>
		/// <param name="users">The users who are allowed to have this role.</param>
		[Obsolete("Creating a PseudoIntegratedRole from an existing role is not advised. You should manually create RoleTemplateData and call the ctor that takes in that object.")]
		public PseudoIntegratedRole(DiscordRole role, BotContext context, List<ulong> users = null, bool enforceMembership = true, RoleComparisonFlags? propsToEnforce = null) {
			Role = role;
			Context = context;
			TemplateData = RoleTemplate.CreateFromRole(context, role);
			UsersSupposedToHaveThisRole = users ?? new List<ulong>();
			EnforceMembership = enforceMembership;
			PropertiesToEnforce = propsToEnforce;
			Complete = true;

			Initialize();
			XanBotLogger.WriteLine("§2Initialized managed role [" + Role.ToString() + "]");
		}

		/// <summary>
		/// Create a new managed role from the specified role ID, context, and optional array of users allowed to have this role.
		/// </summary>
		/// <param name="roleId">The ID of the role to target. This will fail and return and incomplete object if a role with this ID was not found.</param>
		/// <param name="context">The context this role exists in.</param>
		/// <param name="users">The users who are allowed to have this role.</param>
		[Obsolete("Creating a PseudoIntegratedRole from an existing role is not advised. You should manually create RoleTemplateData and call the ctor that takes in that object.")]
		public PseudoIntegratedRole(ulong roleId, BotContext context, List<ulong> users = null, bool enforceMembership = true, RoleComparisonFlags? propsToEnforce = null) {
			DiscordRole role = context.Server.GetRole(roleId);
			if (role == null) {
				XanBotLogger.WriteLine("§4The specified role does not exist under this ID.");
				Complete = false;
				return;
			}

			Role = role;
			Context = context;
			TemplateData = RoleTemplate.CreateFromRole(context, role);
			UsersSupposedToHaveThisRole = users ?? new List<ulong>();
			EnforceMembership = enforceMembership;
			PropertiesToEnforce = propsToEnforce;
			Complete = true;

			Initialize();
			XanBotLogger.WriteLine("§2Initialized managed role [" + Role.ToString() + "]");
		}

		/// <summary>
		/// Create a new managed role from the specified role template and optional array of users allowed to have this role. This constructor may yield if the role does not exist.
		/// </summary>
		/// <param name="users">The users who are allowed to have this role.</param>
		public PseudoIntegratedRole(RoleTemplate template, List<ulong> users = null, bool enforceMembership = true, RoleComparisonFlags? propsToEnforce = null) {
			Role = template.GetOrCreateRole().GetAwaiter().GetResult();
			Context = template.Context;
			TemplateData = template;
			UsersSupposedToHaveThisRole = users ?? new List<ulong>();
			EnforceMembership = enforceMembership;
			PropertiesToEnforce = propsToEnforce;
			Complete = true;

			Initialize();
			XanBotLogger.WriteLine("§2Initialized managed role [" + Role.ToString() + "]");
		}

		/// <summary>
		/// Adds the specified user ID to the list of users allowed to have this role, and then grants them the role.<para/>
		/// Returns true if they were added, and false if they already were registered.
		/// </summary>
		/// <param name="userId"></param>
		/// <returns></returns>
		public async Task<bool> AddToList(ulong userId) {
			DiscordMember asMem = await Context.Server.GetMemberAsync(userId);
			XanBotMember member = XanBotMember.GetMemberFromDiscordMember(asMem);

			if (!EnforceMembership) {
				if (member.HasRole(Role)) {
					return false;
				}
				await member.GrantRoleAsync(Role);
				return true;
			}


			if (UsersSupposedToHaveThisRole.Contains(userId)) return false;
			UsersSupposedToHaveThisRole.Add(userId);
			XanBotLogger.WriteLine("§2Added managed role to member " + member.FullName);
			await member.GrantRoleAsync(Role, "Pseudo-Integrated Role Managed State :: User is now required to have this role.");
			return true;
		}

		/// <summary>
		/// Removes the specified user ID from the list of users allowed to have this role, and then removes the role from said user.<para/>
		/// Returns true if they were removed, and false if they already were not in the list.
		/// </summary>
		/// <param name="userId"></param>
		/// <returns></returns>
		public async Task<bool> RemoveFromList(ulong userId) {
			DiscordMember asMem = await Context.Server.GetMemberAsync(userId);
			XanBotMember member = XanBotMember.GetMemberFromDiscordMember(asMem);

			if (!EnforceMembership) {
				if (!member.HasRole(Role)) {
					return false;
				}
				await member.RevokeRoleAsync(Role);
				return true;
			}

			if (!UsersSupposedToHaveThisRole.Contains(userId)) return false;
			UsersSupposedToHaveThisRole.Remove(userId);
			XanBotLogger.WriteLine("§2Removed managed role from member " + member.FullName);
			await member.RevokeRoleAsync(Role, "Pseudo-Integrated Role Managed State :: User is no longer authorized to have this role.");
			return true;
		}

		/// <summary>
		/// Connects events that listen to roles changing.
		/// </summary>
		private void Initialize() {
			DiscordClient client = XanBotCoreSystem.Client;

			bool dropComparisons = false;
			client.GuildRoleUpdated += async evt => {
				if (dropComparisons) return;

				if (evt.RoleAfter.Id == Role.Id && PropertiesToEnforce != RoleComparisonFlags.None) {
					if (!IsRoleInformationCorrect(evt.RoleAfter)) {
						XanBotLogger.WriteDebugLine("Controlled info for this role is not correct. Changing role settings.");
						dropComparisons = true;
						await evt.RoleAfter.ModifyAsync(role => {
							if (PropertiesToEnforce.Value.HasFlag(RoleComparisonFlags.Name)) role.Name = TemplateData.Name;
							if (PropertiesToEnforce.Value.HasFlag(RoleComparisonFlags.Hoisted)) role.Hoist = TemplateData.IsHoisted;
							if (PropertiesToEnforce.Value.HasFlag(RoleComparisonFlags.Mentionable)) role.Mentionable = TemplateData.IsMentionable;
							if (PropertiesToEnforce.Value.HasFlag(RoleComparisonFlags.Permissions)) role.Permissions = TemplateData.Permissions;
							if (PropertiesToEnforce.Value.HasFlag(RoleComparisonFlags.Color)) role.Color = TemplateData.Color;
							role.AuditLogReason = "Pseudo-Integrated Role Managed State :: Managed roles control their own properties.";
						});
						if (PropertiesToEnforce.Value.HasFlag(RoleComparisonFlags.Position)) {
							if (TemplateData.Position.HasValue) {
								await evt.RoleAfter.ModifyPositionAsync(TemplateData.Position.Value, "Pseudo-Integrated Role Managed State :: Managed roles control their own properties.");
							}
						}
						dropComparisons = false;
					}
				}
			};

			client.GuildRoleDeleted += async evt => {
				if (evt.Role == Role) {
					XanBotLogger.WriteLine("§4Managed role [" + Role.ToString() + "] was deleted. Recreating it and reassigning all users to it.");
					await OnRoleDeleted();
				}
			};

			client.GuildMemberUpdated += async evt => {
				await UpdateRoleMembershipFor(XanBotMember.GetMemberFromDiscordMember(evt.Member));
			};
		}

		/// <summary>
		/// Tests whether or not the specified role's data is identical to what this <see cref="PseudoIntegratedRole"/> wants to control as specified by <see cref="PropertiesToEnforce"/><para/>
		/// If <paramref name="useTemplateComparisonMethod"/> is true, it will NOT use <see cref="PropertiesToEnforce"/> and will instead return the value of <see cref="TemplateData"/>'s RolesEqual method when comparing <paramref name="role"/>
		/// </summary>
		/// <param name="role"></param>
		/// <param name="useTemplateComparisonMethod"></param>
		/// <returns></returns>
		private bool IsRoleInformationCorrect(DiscordRole role, bool useTemplateComparisonMethod = false) {
			if (useTemplateComparisonMethod) {
				return TemplateData.RolesEqual(role);
			}

			if (PropertiesToEnforce.Value.HasFlag(RoleComparisonFlags.Name) && TemplateData.Name != null) {
				if (role.Name != TemplateData.Name) return false;
			}
			if (PropertiesToEnforce.Value.HasFlag(RoleComparisonFlags.Color) && TemplateData.Color.HasValue) {
				if (role.Color.Value != TemplateData.Color.Value.Value) return false;
			}

			if (PropertiesToEnforce.Value.HasFlag(RoleComparisonFlags.Position) && TemplateData.Position.HasValue) {
				if (role.Position != TemplateData.Position.Value) return false;
			}

			if (PropertiesToEnforce.Value.HasFlag(RoleComparisonFlags.Mentionable) && TemplateData.IsMentionable.HasValue) {
				if (role.IsMentionable != TemplateData.IsMentionable.Value) return false;
			}

			if (PropertiesToEnforce.Value.HasFlag(RoleComparisonFlags.Hoisted) && TemplateData.IsHoisted.HasValue) {
				if (role.IsHoisted != TemplateData.IsHoisted.Value) return false;
			}

			if (PropertiesToEnforce.Value.HasFlag(RoleComparisonFlags.Permissions) && TemplateData.Permissions.HasValue) {
				if ((uint)role.Permissions != (uint)TemplateData.Permissions.Value) return false;
			}

			return true;
		}

		/// <summary>
		/// Ensures that all members in this server are compliant by granting (or removing) this role from them.<para/>
		/// This method will take a very long time to complete if it is awaited.<para/>
		/// NOTE: If <see cref="EnforceMembership"/> is false, this method will do nothing.
		/// </summary>
		/// <returns></returns>
		public async Task EnforceAllUsersCompliant() {
			if (!EnforceMembership) return;
			IReadOnlyCollection<DiscordMember> allMembers = await Context.Server.GetAllMembersAsync();
			foreach (DiscordMember dmember in allMembers) {
				XanBotMember member = XanBotMember.GetMemberFromDiscordMember(dmember);
				await UpdateRoleMembershipFor(member);
				await Task.Delay(200); // I want to give Discord some breathing room.
			}
		}

		/// <summary>
		/// Runs when this managed role is deleted.
		/// </summary>
		/// <returns></returns>
		internal async Task OnRoleDeleted() {
			XanBotLogger.WriteLine($"§4Managed role §6{TemplateData.Name} §2deleted. Recreating...");
			Role = await TemplateData.GetOrCreateRole("Pseudo-Integrated Role Managed State :: Managed roles can not be deleted.");

			// TO DO: FIX UNSAFE CASE
			// If the role is deleted again when this loop is running, it will wreak havoc on the already running operation.
			// I need to add a catch case to prevent this.
			foreach (ulong id in UsersSupposedToHaveThisRole) {
				DiscordMember asMem = await Context.Server.GetMemberAsync(id);
				XanBotMember member = XanBotMember.GetMemberFromDiscordMember(asMem);
				await UpdateRoleMembershipFor(member);
			}
			XanBotLogger.WriteLine($"§2Successfully reinstantiated managed role §6{TemplateData.Name}.");
		}

		/// <summary>
		/// Determines whether or not the specified member should have this role via testing usage requirements.
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public bool ShouldMemberHaveRole(XanBotMember member) {
			bool extraUsageIsNull = ExtraUsageRequirements == null;
			bool extraUsageIsTrue = (!extraUsageIsNull) && ExtraUsageRequirements.Invoke(member);
			bool extraUsageIsTrueOrNull = extraUsageIsNull || extraUsageIsTrue;
			bool isOnTheList = UsersSupposedToHaveThisRole.Contains(member.Id);

			if (MembershipDeterminationMethod == MembershipBehavior.ListOnly) {
				return isOnTheList;
			} else if (MembershipDeterminationMethod == MembershipBehavior.MethodOnly) {
				return extraUsageIsTrue;
			} else if (MembershipDeterminationMethod == MembershipBehavior.ListAndMethod) {
				return isOnTheList && extraUsageIsTrue;
			} else if (MembershipDeterminationMethod == MembershipBehavior.ListOrMethod) {
				return isOnTheList || extraUsageIsTrue;
			} else if (MembershipDeterminationMethod == MembershipBehavior.ListAndPotentiallyNullMethod) {
				return isOnTheList && extraUsageIsTrueOrNull;
			} else if (MembershipDeterminationMethod == MembershipBehavior.ListOrPotentiallyNullMethod) {
				return isOnTheList || extraUsageIsTrueOrNull;
			}
			return false;
		}

		/// <summary>
		/// Calls <see cref="ShouldMemberHaveRole(XanBotMember)"/> on the specified member, and if any action is necessary, will add or remove the role from the user.<para/>
		/// NOTE: If <see cref="EnforceMembership"/> is false, this method does nothing.
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public async Task UpdateRoleMembershipFor(XanBotMember member) {
			if (!EnforceMembership) return;
			bool shouldHave = ShouldMemberHaveRole(member);
			bool doesHave = member.HasRole(Role);
			if (shouldHave != doesHave) {
				// state desync!
				if (shouldHave) {
					// doesHave is implicitly false here.
					await member.GrantRoleAsync(Role, "Pseudo-Integrated Role Managed State :: User is required to have this role.");
					XanBotLogger.WriteLine($"§2Added managed role [{Role.Name}] to member {member.FullName}");
				} else {
					// doesHave is implicitly true here.
					await member.RevokeRoleAsync(Role, "Pseudo-Integrated Role Managed State :: User is not authorized to have this role.");
					XanBotLogger.WriteLine($"§2Removed managed role [{Role.Name}] from member {member.FullName}");
				}
			}
		}

		public static explicit operator DiscordRole(PseudoIntegratedRole thisRole) {
			if (!thisRole.Complete) throw new InvalidCastException("This PseudoIntegratedRole is not a complete object (it has been corrupt or was unable to be properly instantiated.)");
			return thisRole.Role;
		}
	}

	/// <summary>
	/// Represents a bare-bones role intended to store template data.
	/// </summary>
	public class RoleTemplate {

		/// <summary>
		/// The <see cref="BotContext"/> that contains this role.
		/// </summary>
		public BotContext Context { get; }

		/// <summary>
		/// The name of this role, or null if it does not manage its name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// The color of this role, or null if it uses the default color or does not manage its color.
		/// </summary>
		public DiscordColor? Color { get; }

		/// <summary>
		/// The position of this role in the list, or null if it's not enforced.
		/// </summary>
		public int? Position { get; }

		/// <summary>
		/// Whether or not this role can be mentioned, or null if it's not enforced.
		/// </summary>
		public bool? IsMentionable { get; }

		/// <summary>
		/// Whether or not this role is hoisted (shows as its own role in the member list vs. just applies a color), or null if it's not enforced.
		/// </summary>
		public bool? IsHoisted { get; }

		/// <summary>
		/// The permissions associated with this role, or null if no permissions are enforced.
		/// </summary>
		public DSharpPlus.Permissions? Permissions { get; }

		/// <summary>
		/// The method in which the data for this role is compared to existing roles in the server in order to determine if the applicable role has been created already. Its default value is <see cref="RoleComparisonFlags.All"/><para/>
		/// Any properties that are null in this <see cref="RoleTemplate"/> will not be checked regardless of what this value dictates.
		/// </summary>
		public RoleComparisonFlags ComparisonMethod { get; set; }

		/// <summary>
		/// Create a new role template from the specified data. Any parameters that are specified as null are not enforced.
		/// </summary>
		/// <param name="context">The <see cref="BotContext"/> this role exists in.</param>
		/// <param name="name">The name of the role.</param>
		/// <param name="color">The color of the role.</param>
		/// <param name="position">The position of the role in the role list.</param>
		/// <param name="mentionable">Whether or not this role is mentionable.</param>
		/// <param name="hoisted">Whether or not this role shows members in their own category (Show members separately)</param>
		/// <param name="permissions">The permissions associated with this role.</param>
		public RoleTemplate(BotContext context, string name = null, DiscordColor? color = null, int? position = null, bool? mentionable = null, bool? hoisted = null, DSharpPlus.Permissions? permissions = null, RoleComparisonFlags comparisonMethod = RoleComparisonFlags.All) {
			Context = context;
			Name = name;
			Color = color;
			Position = position;
			IsMentionable = mentionable;
			IsHoisted = hoisted;
			Permissions = permissions;
			ComparisonMethod = comparisonMethod;
		}

		/// <summary>
		/// Creates a new <see cref="RoleTemplate"/> from the given <see cref="DiscordRole"/>
		/// </summary>
		/// <param name="role"></param>
		/// <returns></returns>
		public static RoleTemplate CreateFromRole(BotContext context, DiscordRole role) {
			return new RoleTemplate(context, role.Name, role.Color, role.Position, role.IsMentionable, role.IsHoisted, role.Permissions);
		}

		/// <summary>
		/// Creates a new <see cref="RoleTemplate"/> from the specified string (which was presumably created with <see cref="TranslateToConfigData"/>).
		/// </summary>
		/// <param name="cfgData"></param>
		/// <returns></returns>
		public static RoleTemplate CreateFromConfigData(string cfgData) {
			// To-do: Use newtonsoft on this class?
			Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(cfgData);
			if (!data.ContainsKey("ServerId")) throw new MalformedConfigDataException("The specified role data does not contain necessary property \"ServerId\".");

			DiscordGuild server = XanBotCoreSystem.Client.GetGuildAsync(ulong.Parse(data["ServerId"].ToString())).GetAwaiter().GetResult();
			BotContext context = BotContextRegistry.GetContext(server);
			string name = (string)data.GetOrDefault("Name", null);
			object ocolor = data.GetOrDefault("Color", null);
			object oposition = data.GetOrDefault("Position", null);
			object omentionable = data.GetOrDefault("Mentionable", null);
			object ohoisted = data.GetOrDefault("Hoisted", null);
			object opermissions = data.GetOrDefault("Permissions", null);
			uint comparisonMethod = uint.Parse(data.GetOrDefault("ComparisonMethod", (uint)RoleComparisonFlags.All).ToString());

			int? color = null;
			int? position = null;
			bool? mentionable = null;
			bool? hoisted = null;
			DSharpPlus.Permissions? permissions = null;
			if (ocolor != null) color = int.Parse(ocolor.ToString());
			if (oposition != null) position = int.Parse(ocolor.ToString());
			if (omentionable != null) mentionable = bool.Parse(omentionable.ToString());
			if (ohoisted != null) hoisted = bool.Parse(ohoisted.ToString());
			if (opermissions != null) permissions = (DSharpPlus.Permissions)uint.Parse(opermissions.ToString());

			return new RoleTemplate(context, name, color, position, mentionable, hoisted, permissions, (RoleComparisonFlags)comparisonMethod);
		}

		/// <summary>
		/// Translates this <see cref="RoleTemplate"/> into a JSON string intended to be populated as config data that can be read by <see cref="CreateFromConfigData(string)"/>
		/// </summary>
		/// <returns></returns>
		public string TranslateToConfigData() {
			// To-do: Use newtonsoft on this class?
			string json = "{";
			json += "\"ServerId\":" + Context.ServerId;
			if (Name != null) json += "\"Name\":" + Name;
			if (Color.HasValue) json += ",\"Color\":" + Color.Value.Value;
			if (Position.HasValue) json += ",\"Position\":" + Position.Value;
			if (IsMentionable.HasValue) json += ",\"Mentionable\":" + IsMentionable.Value;
			if (IsHoisted.HasValue) json += ",\"Hoisted\":" + IsHoisted.Value;
			if (Permissions.HasValue) json += ",\"Permissions\":" + (uint)Permissions.Value;
			json += ",\"ComparisonMethod\":" + (uint)ComparisonMethod;
			json += "}";
			return json;
		}

		/// <summary>
		/// Creates a new role in <see cref="Context"/> with this <see cref="RoleTemplate"/>'s data. This does not test if the role exists already and will always create a new role.<para/>
		/// Consider using <see cref="GetOrCreateRole(string)"/> if you want to get an existing role if it already exists.
		/// </summary>
		/// <returns></returns>
		public async Task<DiscordRole> CreateAsNewRole(string reason = null) {
			DiscordRole role = await Context.Server.CreateRoleAsync(Name, Permissions, Color, IsHoisted, IsMentionable, reason);
			if (Position.HasValue) await role.ModifyPositionAsync(Position.Value, reason);
			return role;
		}

		/// <summary>
		/// Attempts to find a role that has matching data in <see cref="Context"/>. Returns null if it doesn't exist.
		/// </summary>
		/// <returns></returns>
		public DiscordRole GetRole() {
			foreach (DiscordRole role in Context.Server.Roles.Values) {
				if (RolesEqual(role)) return role;
			}
			return null;
		}

		/// <summary>
		/// Attempts to find a role that has matching data in <see cref="Context"/>. If a role that fits this <see cref="RoleTemplate"/>'s information is not found, it will create a new <see cref="DiscordRole"/> with the applicable information.
		/// </summary>
		/// <returns></returns>
		public async Task<DiscordRole> GetOrCreateRole(string creationReason = null) {
			DiscordRole role = GetRole();
			if (role != null) return role;
			return await CreateAsNewRole(creationReason);
		}

		/// <summary>
		/// Returns whether or not the roles' properties are equal. Any null properties on this object are not checked.<para/>
		/// What properties are checked vs what properties are skipped is determined based on <see cref="ComparisonMethod"/> flags.
		/// </summary>
		/// <param name="otherRole"></param>
		/// <returns></returns>
		public bool RolesEqual(DiscordRole otherRole) {
			if (ComparisonMethod == RoleComparisonFlags.None) return false;

			if (ComparisonMethod.HasFlag(RoleComparisonFlags.Name)) {
				if (Name != null && (otherRole.Name != Name)) return false;
			}

			if (ComparisonMethod.HasFlag(RoleComparisonFlags.Color)) {
				if (Color.HasValue && (otherRole.Color.Value != Color.Value.Value)) return false;
			}

			if (ComparisonMethod.HasFlag(RoleComparisonFlags.Position)) {
				if (Position.HasValue && (otherRole.Position != Position.Value)) return false;
			}

			if (ComparisonMethod.HasFlag(RoleComparisonFlags.Mentionable)) {
				if (IsMentionable.HasValue && (otherRole.IsMentionable != IsMentionable)) return false;
			}

			if (ComparisonMethod.HasFlag(RoleComparisonFlags.Hoisted)) {
				if (IsHoisted.HasValue && (otherRole.IsHoisted != IsHoisted)) return false;
			}

			if (ComparisonMethod.HasFlag(RoleComparisonFlags.Permissions)) {
				if (Permissions.HasValue && ((uint)otherRole.Permissions != (uint)Permissions.Value)) return false;
			}

			return true;
		}
	}

	public enum MembershipBehavior {
		/// <summary>
		/// This only checks <see cref="UsersSupposedToHaveThisRole"/>. If the user is not in this list, they cannot have this role. 
		/// This completely ignores the existence of <see cref="ExtraUsageRequirements"/>
		/// </summary>
		ListOnly,

		/// <summary>
		/// This only checks <see cref="ExtraUsageRequirements"/>.<para/>
		/// If <see cref="ExtraUsageRequirements"/> is null, then <see cref="ShouldMemberHaveRole(XanBotMember)"/> will always return false.
		/// </summary>
		MethodOnly,

		/// <summary>
		/// This checks if the user is in <see cref="UsersSupposedToHaveThisRole"/> AND if <see cref="ExtraUsageRequirements"/> returns true. If <see cref="ExtraUsageRequirements"/> is null, <see cref="ShouldMemberHaveRole(XanBotMember)"/> will always return false.<para/>
		/// If <see cref="ExtraUsageRequirements"/> can be null, consider using <see cref="ListAndPotentiallyNullMethod"/>, which behaves like <see cref="ExtraUsageRequirements"/> returns true if it is not defined.
		/// </summary>
		ListAndMethod,

		/// <summary>
		/// This checks if the user is in <see cref="UsersSupposedToHaveThisRole"/> OR if <see cref="ExtraUsageRequirements"/> returns true. If <see cref="ExtraUsageRequirements"/> is null, then this option behaves identically to <see cref="ListOnly"/> (since it's not possible to check the method)<para/>
		/// If <see cref="ExtraUsageRequirements"/> can be null, consider using <see cref="ListOrPotentiallyNullMethod"/>, which behaves like <see cref="ExtraUsageRequirements"/> returns true if it is not defined.
		/// </summary>
		ListOrMethod,

		/// <summary>
		/// This checks if the user is in <see cref="UsersSupposedToHaveThisRole"/> AND if <see cref="ExtraUsageRequirements"/> returns true or is null.
		/// </summary>
		ListAndPotentiallyNullMethod,

		/// <summary>
		/// This checks if the user is in <see cref="UsersSupposedToHaveThisRole"/> OR if <see cref="ExtraUsageRequirements"/> returns true or is null.
		/// </summary>
		ListOrPotentiallyNullMethod
	}

	/// <summary>
	/// Determines how much data is checked when comparing two roles for a <see cref="RoleTemplate"/>
	/// </summary>
	[Flags]
	public enum RoleComparisonFlags {

		/// <summary>
		/// Compare nothing. IMPORTANT NOTE: This will cause <see cref="RoleTemplate"/> comparison method to always return FALSE (NOT true!)
		/// </summary>
		None = 0,

		/// <summary>
		/// Compare the name of both roles.
		/// </summary>
		Name = 1 << 0,

		/// <summary>
		/// Compare the color of both roles.
		/// </summary>
		Color = 1 << 1,

		/// <summary>
		/// Compare the roles' Mentionable setting.
		/// </summary>
		Mentionable = 1 << 2,

		/// <summary>
		/// Compare the roles' Hoisted settings ("Show this role separately from other roles")
		/// </summary>
		Hoisted = 1 << 3,

		/// <summary>
		/// Compare the roles' permissions for equality.
		/// </summary>
		Permissions = 1 << 4,

		/// <summary>
		/// Compare the roles' positions for equality.
		/// </summary>
		Position = 1 << 5,

		/// <summary>
		/// Identical to employing all comparison methods at once, which compares name, color, mentionable, hoisted, position, and all permissions
		/// </summary>
		All = 0b11111,
	}
}
