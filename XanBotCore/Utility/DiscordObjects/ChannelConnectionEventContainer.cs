#pragma warning disable CS1998
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XanBotCore.Logging;
using XanBotCore.UserObjects;

namespace XanBotCore.Utility.DiscordObjects {
	/// <summary>
	/// Some added extras to VoiceNext that adds events pertaining to when a user joins or leaves a channel.
	/// </summary>
	public class ChannelConnectionEventContainer {

		private static readonly Dictionary<DiscordChannel, ChannelConnectionEventContainer> BindingCache = new Dictionary<DiscordChannel, ChannelConnectionEventContainer>();

		/// <summary>
		/// The <see cref="DiscordChannel"/> this <see cref="ChannelConnectionEventContainer"/> and its events targets.
		/// </summary>
		public DiscordChannel Channel { get; }

		private List<DiscordMember> LastCheckedChannelUsers = new List<DiscordMember>();

		private ChannelConnectionEventContainer(DiscordChannel channel) {
			Channel = channel;
			BindingCache[channel] = this;
			LastCheckedChannelUsers = Channel.Users.ToList();

			XanBotCoreSystem.Client.VoiceStateUpdated += async evt => {
				XanBotLogger.WriteDebugLine("Voice state updated.");
				bool oldHas = false;
				bool newHas = false;

				List<DiscordMember> currentUsers = Channel.Users.ToList();
				if (!LastCheckedChannelUsers.ContentEquals(currentUsers)) {
					XanBotLogger.WriteDebugLine("Some member changed.");
					oldHas = LastCheckedChannelUsers.Contains(evt.User);
					newHas = currentUsers.Contains(evt.User);
				}
				LastCheckedChannelUsers = currentUsers;

				XanBotLogger.WriteDebugLine("User WAS in channel: " + oldHas);
				XanBotLogger.WriteDebugLine("User IS in channel: " + newHas);
				//XanBotLogger.WriteDebugLine("If both are true, then the member likely changed their state e.g. muted/unmuted etc.");
				//XanBotLogger.WriteDebugLine("If both are false, then the member changed voice channels and wasn't in this one before nor are they coming into this one.");
				if (oldHas != newHas) {
					// Something changed...
					if (!oldHas && newHas) {
						// User was not here, now they are.
						try {
							OnMemberConnected(evt.User);
							OnMemberConnectionChanged(evt.User, ConnectionType.Connected);
						} catch { }
						XanBotLogger.WriteDebugLine("Fired join for " + evt.User.GetFullName());
					} else {
						// User was here, now they aren't. Disconnect.
						try {
							OnMemberDisconnected(evt.User);
							OnMemberConnectionChanged(evt.User, ConnectionType.Disconnected);
						} catch { }
						XanBotLogger.WriteDebugLine("Fired leave for " + evt.User.GetFullName());
					}
				}
			};
		}

		/// <summary>
		/// Creates a new <see cref="ChannelConnectionEventContainer"/> (or gets an existing one) for this channel.<para/>
		/// Consider implementing a using statement for <see cref="VoiceNextChannelConnectionEvents"/> and then calling <see cref="VoiceNextChannelConnectionEvents.GetConnectionEvents(DiscordChannel)"/> on a <see cref="DiscordChannel>"/>
		/// </summary>
		/// <param name="channel"></param>
		/// <exception cref="ArgumentException">If the specified <see cref="DiscordChannel"/>'s <see cref="DiscordChannel.Type"/> is NOT <see cref="ChannelType.Voice"/></exception>
		/// <exception cref="InvalidOperationException">If a <see cref="VoiceNextExtension"/> has not been created.</exception>
		internal static ChannelConnectionEventContainer GetBindingFor(DiscordChannel channel) {
			if (XanBotCoreSystem.VoiceClient == null) throw new InvalidOperationException("Cannot use connection bindings if the bot can't recognize voice channels in the first place.");
			if (channel.Type != ChannelType.Voice) throw new ArgumentException("Expected a voice channel.", "channel");

			if (BindingCache.TryGetValue(channel, out ChannelConnectionEventContainer retn)) return retn;
			return new ChannelConnectionEventContainer(channel);
		}

		/// <summary>
		/// Runs when a user connects or disconnects to or from the parent <see cref="DiscordChannel"/>
		/// </summary>
		public event MemberConnectionChangedEvent OnMemberConnectionChanged;
		public delegate void MemberConnectionChangedEvent(DiscordUser user, ConnectionType type);

		/// <summary>
		/// Runs when a user connects to the parent <see cref="DiscordChannel"/>
		/// </summary>
		public event MemberConnectedEvent OnMemberConnected;
		public delegate void MemberConnectedEvent(DiscordUser user);

		/// <summary>
		/// Runs when a user disconnects from the parent <see cref="DiscordChannel"/>
		/// </summary>
		public event MemberDisconnectedEvent OnMemberDisconnected;
		public delegate void MemberDisconnectedEvent(DiscordUser user);


		public enum ConnectionType {
			Connected, Disconnected
		}
	}

	public static class VoiceNextChannelConnectionEvents {

		/// <summary>
		/// Returns a container for events that represent when users connect or disconnect to or from this channel.
		/// </summary>
		/// <param name="channel"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		public static ChannelConnectionEventContainer GetConnectionEvents(this DiscordChannel channel) {
			return ChannelConnectionEventContainer.GetBindingFor(channel);
		}
	}

}
