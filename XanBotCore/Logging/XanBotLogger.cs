using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using XanBotCore.DataPersistence;

namespace XanBotCore.Logging {

	/// <summary>
	/// A console logging utility. Offers the ability to format messages with color codes.
	/// </summary>
	public class XanBotLogger {

		/// <summary>
		/// The symbol recognized in messages for color codes. This is identical to Minecraft's color code system.<para/>
		/// See <a href="https://minecraft.gamepedia.com/Formatting_codes#Color_codes">https://minecraft.gamepedia.com/Formatting_codes#Color_codes</a> for more information.
		/// </summary>
		public const char COLOR_CODE_SYM = '§';

		/// <summary>
		/// If true, the logger will print debug messages even if <see cref="XanBotCoreSystem.IsDebugMode"/> is false. If <see cref="XanBotCoreSystem.IsDebugMode"/> is true, this will do nothing.
		/// </summary>
		public static bool ShowDebugMessagesAnyway { get; set; } = false;

		/// <summary>
		/// The time when this class is initialized into memory. Used for the log file name. This value does not change.
		/// </summary>
		private static readonly string CLASS_INIT_TIMESTAMP = DateTime.UtcNow.ToFileTime().ToString();

		/// <summary>
		/// The folder that the log file is stored in as a string. Default value is .\ (current EXE directory).<para/>
		/// This can only be set BEFORE calling <see cref="XanBotCoreSystem.InitializeBotAsync(string, bool, bool, bool)"/>. Attempting to set it after calling this will throw an <see cref="InvalidOperationException"/>
		/// </summary>
		/// <exception cref="InvalidOperationException"/>
		public static string LogContainerFolder {
			get {
				return LogFilePathInternal;
			}
			set {
				if (IsPathLocked) throw new InvalidOperationException("Cannot set the log file path after calling XanBotCoreSystem initialize method.");
				LogFilePathInternal = value;

				if (!LogFilePathInternal.EndsWith("\\")) {
					LogFilePathInternal += "\\";
				}
			}
		}
		internal static string LogFilePathInternal = ".\\";
		internal static bool IsPathLocked = false;

		/// <summary>
		/// The name of the current log file that this <see cref="XanBotLogger"/> is writing to.<para/>
		/// This is equal to <see cref="LogContainerFolder"/> + "logfile-" + <see cref="CLASS_INIT_TIMESTAMP"/> + ".log";
		/// </summary>
		public static string LogFilePath => LogContainerFolder + "logfile-" + CLASS_INIT_TIMESTAMP + ".log";

		/// <summary>
		/// The current console log in a single string.
		/// </summary>
		private static string Log = "";

		/// <summary>
		/// The default color used by the console for messages printed with no color. Default value is <see cref="ConsoleColor.Green"/><para/>
		/// This can be set to a <see cref="ConsoleColorVT"/> or a <see cref="ConsoleColor"/>. Attempting to set it to a <see cref="ConsoleColorVT"/> when <see cref="IsVTEnabled"/> is false will throw a <see cref="NotSupportedException"/>.
		/// </summary>
		/// <exception cref="NotSupportedException"/>
		public static ConsoleColorVT DefaultColor {
			get => DefaultColorInternal;
			set {
				if (value.GetType() != typeof(ConsoleColor)) {
					if (!IsVTEnabled) throw new NotSupportedException("Cannot set DefaultColor to ConsoleColorVT -- VT is not enabled. Did you remember to call EnableVTSupport()?");
				}
				DefaultColorInternal = value;
			}
		}
		private static ConsoleColorVT DefaultColorInternal = ConsoleColor.Green;

		/// <summary>
		/// A map of byte code values to ConsoleColors
		/// </summary>
		public static readonly IReadOnlyDictionary<byte, ConsoleColor> ConsoleColorMap = new Dictionary<byte, ConsoleColor> {
			[0] = ConsoleColor.Black,
			[1] = ConsoleColor.DarkBlue,
			[2] = ConsoleColor.DarkGreen,
			[3] = ConsoleColor.DarkCyan,
			[4] = ConsoleColor.DarkRed,
			[5] = ConsoleColor.DarkMagenta,
			[6] = ConsoleColor.DarkYellow,
			[7] = ConsoleColor.DarkGray,
			[8] = ConsoleColor.Gray,
			[9] = ConsoleColor.Blue,
			[10] = ConsoleColor.Green,
			[11] = ConsoleColor.Cyan,
			[12] = ConsoleColor.Red,
			[13] = ConsoleColor.Magenta,
			[14] = ConsoleColor.Yellow,
			[15] = ConsoleColor.White
		};

		/// <summary>
		/// An orange console color created via the RGB code (255, 128, 0).
		/// </summary>
		private static readonly ConsoleColorVT ORANGE = new ConsoleColorVT(255, 128, 0);

		/// <summary>
		/// An yellowy color.
		/// </summary>
		private static readonly ConsoleColorVT YELLA = new ConsoleColorVT(255, 183, 66);

		/// <summary>
		/// An super dark red color.
		/// </summary>
		private static readonly ConsoleColorVT BLOOD = new ConsoleColorVT(15, 0, 0);

		private static ConsoleColorVT FGColorInternal = ConsoleColor.White;
		private static ConsoleColorVT BGColorInternal = ConsoleColor.Black;

		/// <summary>
		/// The foreground color of this console.<para/>
		/// Setting this to null if VT is enabled will default to white.<para/>
		/// Attempting to set this value to a <see cref="ConsoleColorVT"/> if <see cref="IsVTEnabled"/> is false will round the color to the nearest <see cref="ConsoleColor"/>
		/// </summary>
		/// <exception cref="NotSupportedException"/>
		public static ConsoleColorVT ForegroundColor {
			get => FGColorInternal;
			set {
				if (!IsVTEnabled) {
					ConsoleColor asStock;
					if (value == null) {
						asStock = ConsoleColor.Black;
					} else {
						asStock = value.GetNearestConsoleColor();
					}

					Console.ForegroundColor = asStock;
					value = asStock;
				}
				if (value == null) value = ConsoleColor.White;
				FGColorInternal = value;
				value.ApplyToForeground();
			}
		}

		/// <summary>
		/// The background color of this console.<para/>
		/// Setting this to null if VT is enabled will default to black.<para/>
		/// Attempting to set this value to a <see cref="ConsoleColorVT"/> if <see cref="IsVTEnabled"/> is false will round the color to the nearest <see cref="ConsoleColor"/>
		/// </summary>
		/// <exception cref="NotSupportedException"/>
		public static ConsoleColorVT BackgroundColor {
			get => BGColorInternal;
			set {
				if (!IsVTEnabled) {
					ConsoleColor asStock;
					if (value == null) {
						asStock = ConsoleColor.White;
					} else {
						asStock = value.GetNearestConsoleColor();
					}

					Console.ForegroundColor = asStock;
					value = asStock;
				}
				if (value == null) value = ConsoleColor.Black;
				BGColorInternal = value;
				value.ApplyToBackground();
			}
		}

		#region Kernel32 API Imports
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr GetStdHandle(int nStdHandle);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

		static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
		#endregion

		/// <summary>
		/// Whether or not VT Sequences are enabled and should be used.
		/// </summary>
		public static bool IsVTEnabled { get; private set; } = false;

		/// <summary>
		/// When called, this enables VT Sequence support for the console. Whether or not this action will be successful depends on the platform this bot is running on.<para/>
		/// This method will return <see cref="true"/> if VT sequences are supported and enabled, and <see cref="false"/> if they are not.<para/>
		/// VT sequences allow low level control of the console's colors, including the allowance of full 16-million color RGB text and backgrounds.<para/>
		/// See <a href="https://docs.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences">https://docs.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences</a> for more information.
		/// </summary>
		/// <returns>True if VT sequences are supported, false if they are not.</returns>
		public static bool EnableVTSupport() {
			if (IsVTEnabled) return true;

			IntPtr hOut = GetStdHandle(-11);
			if (hOut != INVALID_HANDLE_VALUE) {
				if (GetConsoleMode(hOut, out uint mode)) {
					mode |= 0x4;
					if (SetConsoleMode(hOut, mode)) {
						IsVTEnabled = true;
						return true;
					}
				}
			}
			return false;
		}

		private static bool HasWarnedForLackOfVT = false;

		/// <summary>
		/// A proxy to Write that does some extra sanity checks.
		/// </summary>
		private static void WriteProxy(string message) {
			message = message.Replace('•', '■'); // Prevent abuse of beeping. If people run commands with bullets, the console will see it as 0x07 BEL and beep.
			if (Regex.IsMatch(message, @"(\x1b)(\[[^m]+)m") && !IsVTEnabled) {
				if (!HasWarnedForLackOfVT) {
					Console.Beep();
					Console.WriteLine("\nWARNING: VT Sequence detected, but VT Sequences are not enabled! This warning will only show once. All VT sequences will be removed.\n");
					HasWarnedForLackOfVT = true;
				}
				Regex.Replace(message, @"(\x1b)(\[[^m]+)m", "");
			}
			Console.Write(message);
		}

		/// <summary>
		/// A proxy to WriteLine that does some extra sanity checks.
		/// </summary>
		/// <param name="info"></param>
		private static void WriteProxyLine(string info) {
			WriteProxy(info + "\n");
		}

		/// <summary>
		/// Returns a formatted timestamp: "[HH:MM:SS] "
		/// </summary>
		/// <returns>Returns a formatted timestamp: "[HH:MM:SS] "</returns>
		public static string GetFormattedTimestamp() {
			TimeSpan currentTime = DateTime.Now.TimeOfDay;
			return "[" + currentTime.Hours.ToString("D2") + ":" + currentTime.Minutes.ToString("D2") + ":" + currentTime.Seconds.ToString("D2") + "] ";
		}

		/// <summary>
		/// Returns a message formatted as an error message for display in text channels.
		/// </summary>
		public static string GetFormattedResponseErrMsg(Exception ex) {
			return "```diff\n-" + ex.GetType().ToString() + "-\n" + ex.Message + "\n" + ex.StackTrace + "```";
		}

		/// <summary>
		/// Writes errors to the console and plays a beep sound to alert the operator.
		/// </summary>
		public static void WriteException(Exception ex) {
			WriteException(ex, false);
		}

		private static void WriteException(Exception ex, bool isFromAggregate) {
			if (ex is AggregateException agg) {
				WriteLine("§c[" + agg.GetType().ToString() + " Thrown!] §eInner Exceptions:\n", !isFromAggregate);
				foreach (Exception sub in agg.InnerExceptions) {
					WriteException(sub, true);
				}
				WriteLine("§c-----------------------------------------------");
			} else {
				if (IsVTEnabled) {
					WriteExceptionVT(ex, isFromAggregate);
					if (!isFromAggregate) Console.Beep();
				} else {
					WriteLine("§c[" + ex.GetType().ToString() + " Thrown!] §e" + ex.Message + "\n§4" + ex.StackTrace + "\n", !isFromAggregate);
				}
			}
		}

		/// <summary>
		/// Identical to <see cref="WriteException(Exception)"/> but it targets VT mode.
		/// </summary>
		/// <param name="ex"></param>
		private static void WriteExceptionVT(Exception ex, bool isFromAggregate = false) {
			XanBotConsoleCore.BumpIncomingLogTextPre();

			ConsoleColorVT old = ForegroundColor;

			ConsoleColorVT red = ConsoleColor.Red;
			ConsoleColorVT darkRed = ConsoleColor.DarkRed;
			ConsoleColorVT darkYellow = ConsoleColor.DarkYellow;
			// 4 and 24 add and remove underline respectively
			ConsoleColorVT oldbg = BackgroundColor;
			BLOOD.ApplyToBackground();
			string msg;
			if (isFromAggregate) {
				msg = darkYellow + GetFormattedTimestamp() + red + "[\x1b[4m" + YELLA + ex.GetType().ToString() + "\x1b[24m" + red + "]: " + ORANGE + ex.Message + "\n" + darkRed + ex.StackTrace + "\n\n";
			} else {
				msg = darkYellow + GetFormattedTimestamp() + red + "[\x1b[4m" + YELLA + ex.GetType().ToString() + " Thrown!\x1b[24m" + red + "]: " + ORANGE + ex.Message + "\n" + darkRed + ex.StackTrace + "\n\n";
			}
			WriteMessageFromColorsVT(msg);

			ForegroundColor = old;
			oldbg.ApplyToBackground();
			LogMessage("[" + ex.GetType().ToString() + " Thrown!] " + ex.Message + "\n" + ex.StackTrace); // Write it to the log file.

			XanBotConsoleCore.BumpIncomingLogTextPost();
		}

		/// <summary>
		/// Logs the string to the log file without showing in the console.
		/// </summary>
		/// <param name="message">The string to log.</param>
		public static void LogMessage(string message) {
			//if (MessageHasColors(message)) {
			message = ClearColorFormattingCodeAll(message);
			//}
			Log += GetFormattedTimestamp() + message + "\n";
			WriteLogFile();
		}

		/// <summary>
		/// Returns `true` if the message contains the color code symbol and, by extension, a color code.
		/// </summary>
		public static bool MessageHasColors(string message) {
			if (IsVTEnabled) {
				return Regex.IsMatch(message, @"(\x1b)(\[[^m]+)m") || message.Contains(COLOR_CODE_SYM);
			}
			return message.Contains(COLOR_CODE_SYM);
		}

		/// <summary>
		/// Strips away all color formatting stuff from a message so that it's just plain text.
		/// </summary>
		public static string StripColorFormattingCode(string message) {
			string[] colorSegs = message.Split(COLOR_CODE_SYM);
			//colorSegs[0] will be empty if we have a color code at the start.
			string res = "";
			foreach (string coloredString in colorSegs) {
				if (coloredString.Length >= 1) {
					if (coloredString == colorSegs.First()) {
						if (message.Substring(0, 1) == COLOR_CODE_SYM.ToString()) {
							res += coloredString.Substring(1);
						} else {
							res += coloredString;
						}
					} else {
						res += coloredString.Substring(1);
					}
				} else {
					res += coloredString;
				}
			}
			return res;
		}

		/// <summary>
		/// Strips away all VT color formatting text.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public static string StripVTColorFormattingCode(string message) {
			// tfw the §# version is a huge block of text but this is just like "nah"
			string withoutx1bs = Regex.Replace(message, @"(\x1b)(\[[^m]+)m", "");
			return Regex.Replace(withoutx1bs, @"((\^#)([0-9]|[a-f]|[A-F]){6};)", "");
		}

		/// <summary>
		/// Calls both formatting cleanup methods
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public static string ClearColorFormattingCodeAll(string message) {
			message = StripVTColorFormattingCode(message);
			return StripColorFormattingCode(message);
		}

		/// <summary>
		/// Writes a color coded console message in a similar manner to text in Minecraft, where colors are selected with § (e.g. §a will make all following text green)
		/// </summary>
		/// <param name="message">The message to write.</param>
		public static void WriteMessageFromColors(string message) {
			WriteMessageFromColors(message, false);
		}

		internal static void WriteMessageFromColors(string message, bool skipConsoleStuff) {
			string[] colorSegs = message.Split(COLOR_CODE_SYM);
			// colorSegs[0] will be empty if we have a color code at the start.
			ConsoleColor defColor = Console.ForegroundColor;
			if (!skipConsoleStuff) {
				XanBotConsoleCore.BumpIncomingLogTextPre();
			}

			foreach (string coloredString in colorSegs) {
				if (coloredString.Length > 1) {
					byte code = 255;
					try { code = Convert.ToByte(coloredString.First().ToString(), 16); } catch (Exception) { }
					bool success = ConsoleColorMap.TryGetValue(code, out ConsoleColor color);
					if (success) {
						if (!IsVTEnabled) {
							Console.ForegroundColor = color;
						} else {
							ForegroundColor = color;
						}
						WriteProxy(coloredString.Substring(1));
					} else {
						if (!skipConsoleStuff) continue;
						WriteProxy(coloredString);
					}
				}
			}

			if (!skipConsoleStuff) {
				Console.ForegroundColor = defColor;
				XanBotConsoleCore.BumpIncomingLogTextPost();
			}
		}

		/// <summary>
		/// Writes a color coded console message via the custom VT formatting (^#......; where ...... is a hex color code)
		/// </summary>
		/// <param name="message"></param>
		public static void WriteMessageFromColorsVT(string message) {
			MatchCollection colorMatches = Regex.Matches(message, ConsoleColorVT.COLOR_CODE_REGEX);
			message = Regex.Replace(message, ConsoleColorVT.COLOR_CODE_REGEX, SUPER_UNIQUE_SPLIT_THINGY[0]); // The behavior of Regex.split bricks everything so I have to use this disgusting hacky method.
			string[] colorSegs = message.Split(SUPER_UNIQUE_SPLIT_THINGY, StringSplitOptions.None);

			// colorSegs[0] will be empty if we have a color code at the start.

			ConsoleColorVT old = ForegroundColor;
			XanBotConsoleCore.BumpIncomingLogTextPre();

			for (int idx = 0; idx < colorSegs.Length; idx++) {
				string coloredString = colorSegs[idx];
				if (coloredString.Contains(COLOR_CODE_SYM)) {
					// Backwards compatibility.
					WriteMessageFromColors(coloredString, true);
				} else {
					WriteProxy(coloredString);
				}

				if (idx < colorMatches.Count)
					ForegroundColor = ConsoleColorVT.FromFormattedString(colorMatches[idx].Value);
			}

			ForegroundColor = old;
			XanBotConsoleCore.BumpIncomingLogTextPost();
		}
		private static readonly string[] SUPER_UNIQUE_SPLIT_THINGY = new string[] { "\x69\x42\x06\x66" };

		/// <summary>
		/// Log some text on a single line, and make a newline afterwards.
		/// </summary>
		/// <param name="message">The text to log.</param>
		/// <param name="alertSound">If true, this message will cause the console to beep.</param>
		/// <param name="isDebugModeOnly">If true, this will only log if the bot is in debug mode.</param>
		public static void WriteLine(string message = "", bool alertSound = false, bool isDebugModeOnly = false) {
			if (isDebugModeOnly && (!XanBotCoreSystem.IsDebugMode && !ShowDebugMessagesAnyway)) return;
			ClearConsoleIfNecessary();
			if (alertSound) Console.Beep();
			LogMessage(message);

			message += "\n";
			string timestamp = GetFormattedTimestamp();
			//string logMessage = message;
			//if (MessageHasColors(logMessage)) logMessage = StripColorFormattingCode(logMessage);
			//Log += timestamp + logMessage;

			if (IsVTEnabled) {
				//WriteLineVT(timestamp, logMessage);
				ConsoleColorVT old = ForegroundColor;
				WriteMessageFromColorsVT("^#008000;" + timestamp + old + message);
			} else {
				message = StripVTColorFormattingCode(message); // Get rid of any VT sequences.
				WriteMessageFromColors(COLOR_CODE_SYM + "2" + timestamp + ForegroundColor.ToStringNonVT(true) + message);
			}
			WriteLogFile();
		}

		/// <summary>
		/// Log some text on a single line
		/// </summary>
		/// <param name="message">The text to log.</param>
		/// <param name="alertSound">If true, this message will cause the console to beep.</param>
		/// <param name="isDebugModeOnly">If true, this will only log if the bot is in debug mode.</param>
		public static void Write(string message = "", bool alertSound = false, bool isDebugModeOnly = false) {
			if (isDebugModeOnly && (!XanBotCoreSystem.IsDebugMode && !ShowDebugMessagesAnyway)) return;
			ClearConsoleIfNecessary();
			if (alertSound) Console.Beep();
			string timestamp = GetFormattedTimestamp();
			LogMessage(message);
			if (IsVTEnabled) {
				WriteMessageFromColorsVT("^#008000;" + timestamp + DefaultColorInternal.ToString(IsVTEnabled) + message);
			} else {
				message = StripVTColorFormattingCode(message); // Get rid of any VT sequences.
				WriteMessageFromColors(COLOR_CODE_SYM + "2" + timestamp + ForegroundColor.ToStringNonVT(true) + message);
			}
			WriteLogFile();
		}

		/// <summary>
		/// An alias method that calls <see cref="WriteLine"/> with its <paramref name="isDebugModeOnly"/> parameter set to true.<para/>
		/// This also enforces the text to be darkgray by default. It is possible to override this normally.
		/// </summary>
		/// <param name="message">The message to display.</param>
		/// <param name="alertSound">If true, this will only log if the bot is in debug mode.</param>
		public static void WriteDebugLine(string message = "", bool alertSound = false) {
			if (IsVTEnabled) {
				WriteDebugLineVT(message, alertSound);
			} else {
				message = "§7" + message;
				message = StripVTColorFormattingCode(message);
				WriteLine(message, alertSound, true);
			}
		}

		private static void WriteDebugLineVT(string message = "", bool alertSound = false) {
			ConsoleColorVT lastColor = ForegroundColor;
			ForegroundColor = ConsoleColor.Gray;
			WriteLine(message, alertSound, true);
			ForegroundColor = lastColor;
		}

		/// <summary>
		/// Write the currently cached program log to the file at <paramref name="path"/>, or the current log file if the path parameter is null.
		/// </summary>
		/// <param name="path">A file path for a log file. This should ideally be a text document with the .log extension.</param>
		public static void WriteLogFile(string path = null) {
			path = path ?? LogFilePath;
			XFileHandler.CreateEntirePathIfDoesntExist(path);
			Stream logFileStream = File.AppendText(path).BaseStream;
			char[] chars = Log.ToCharArray();
			logFileStream.Write(Encoding.UTF8.GetBytes(chars), 0, chars.Length);
			logFileStream.Close();
			Log = ""; //Reset it.
		}

		/// <summary>
		/// In case the console runs out of buffer space, clear it out. This is where log files come in handy because the console has limited display.
		/// </summary>
		public static void ClearConsoleIfNecessary() {
			if (Console.CursorTop >= Console.BufferHeight - 50) {
				Console.Clear();
				if (IsVTEnabled) {
					ConsoleColorVT old = ForegroundColor;
					ForegroundColor = ConsoleColor.DarkCyan;
					WriteConsoleClearedNotification();
					ForegroundColor = old;
				} else {
					ConsoleColor old = Console.ForegroundColor;
					Console.ForegroundColor = ConsoleColor.DarkCyan;
					WriteConsoleClearedNotification();
					Console.ForegroundColor = old;
				}


			}
		}

		/// <summary>
		/// Writes a message notifying the operator that the console has been cleared to give the buffer more space.
		/// </summary>
		private static void WriteConsoleClearedNotification() {
			WriteProxyLine("-- CONSOLE CLEARED TO RESET BUFFER --");
			LogMessage("-- CONSOLE CLEARED TO RESET BUFFER --");
		}


		/// <summary>
		/// Gets the latest log file in its current state as a FileStream. Used by <seealso cref="CommandSystem.Commands.CommandShutdown"/> to post the latest log on bot shutdown.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static FileStream GetLatestLogFileStream(string path = null) {
			path = path ?? LogFilePath;
			return File.OpenRead(path);
		}
	}
}
