using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XanBotCore;
using XanBotCore.Logging;
using XanBotCore.Permissions;

namespace EXAMPLE_BOT {
	class Program {

		private const string BOT_TOKEN = "asdfg";

		private static readonly ConsoleColorVT MTN_DEW_YELLOW = new ConsoleColorVT(128, 255, 63);

		static void ThrowTestException() {
			throw new InvalidOperationException("This is an exception message.");
		}

		/// <summary>
		/// Tests the newly added VT Sequence system.<para/>
		/// VT Sequences can be activated traditionally (\x1b[0;1m), via <see cref="ConsoleColorVT"/> (for easy designation of RGB colors), or via an alias ^#......; where ...... is a hex color code.<para/>
		/// The legacy systems using § will still work too.
		/// </summary>
		static void DoVTConsoleTest() {
			bool supportsVT = XanBotLogger.EnableVTSupport();
			if (!supportsVT) {
				XanBotLogger.ForegroundColor = ConsoleColor.Red;
				XanBotLogger.WriteLine("ooOOoo! scHeT! this compootis does not support VT sequences! Colors will be rounded to the most similar ConsoleColor!");
			}
			XanBotLogger.ForegroundColor = MTN_DEW_YELLOW;
			XanBotLogger.WriteLine("Dhue the dieu. This should be some weird yellow-green color.");
			XanBotLogger.ForegroundColor = ConsoleColor.White;
			XanBotLogger.WriteLine("Nice. If this is white, the backwards compatibility with setting directly to ConsoleColor worked.");
			XanBotLogger.WriteLine("§cAnd if this is red, the legacy color formatting with alt+21 is working.");
			try {
				ThrowTestException();
			} catch (Exception ex) {
				XanBotLogger.WriteException(ex);
			}
			
			XanBotLogger.WriteLine("^#FFFFFF;AdBlo\x1b[4mn\x1b[24mk disabaled.,. You may now press any key to comtue to next ^#FF0000;d^#FF8000;i^#FFFF00;m^#80FF00;e^#00FF00;n^#00FF80;s^#00FFFF;i^#0080FF;o^#0000FF;n^#8000FF;a^#FF00FF;l^#FF0080;s^#FFFFFF;.");
			Console.ReadKey(true);
		}

		static void Main(string[] args) {
			Console.Title = "Optional console title here.";

			DoVTConsoleTest();

			try {
				MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
				XanBotCoreSystem.Exit();
			} catch (Exception ex) {
				XanBotLogger.WriteException(ex);
				XanBotLogger.WriteLine("§1Press any key to quit.");
				Console.ReadKey(true);
				XanBotCoreSystem.Exit(1);
			}
		}

		public static async Task MainAsync(string[] args) {
			// Start the bot init cycle.
			// This method as-is creates a voice client (first boolean argument) and says that the bot is targeting Windows 7 (second bool).
			await XanBotCoreSystem.InitializeBotAsync(BOT_TOKEN, true, true);
			// Do any other stuff that runs after the bot has connected to discord here.

			await Task.Delay(-1);
		}
	}
}
