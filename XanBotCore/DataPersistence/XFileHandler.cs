using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XanBotCore.ServerRepresentation;

namespace XanBotCore.DataPersistence {

	/// <summary>
	/// Handles all filesystem control from the bot system, primarily for use in data persistence on a per-context basis, effectively allowing individual servers to have their own data stores.
	/// </summary>
	public class XFileHandler {
		/// <summary>
		/// The directory to the data persistence storage. Its default value is %appdata%\XanBotCore
		/// </summary>
		public static string BotCoreDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XanBotCore");

		/// <summary>
		/// The cache of existing <see cref="XFileHandler"/>s
		/// </summary>
		private static readonly Dictionary<BotContext, Dictionary<string, XFileHandler>> StoredContextBindings = new Dictionary<BotContext, Dictionary<string, XFileHandler>>();

		/// <summary>
		/// The global handler for when a bot context is null.
		/// </summary>
		public static readonly XFileHandler GLOBAL_HANDLER = new XFileHandler(BotCoreDirectory + "\\GlobalStorageContext\\");

		/// <summary>
		/// The underlying bot context for this <see cref="XFileHandler"/>
		/// </summary>
		public BotContext Context { get; private set; } = null;

		/// <summary>
		/// The underlying path of this <see cref="XFileHandler"/>. This is the directory all files for this handler will go to.
		/// </summary>
		public string BasePath {
			get {
				return BasePathInternal;
			}
			set {
				if (!value.EndsWith(@"\")) {
					value += @"\";
				}
				BasePathInternal = value;
			}
		}
		private string BasePathInternal = "";

		/// <summary>
		/// Construct a new <see cref="XFileHandler"/> in the specified directory and optional tied bot context.
		/// </summary>
		/// <param name="dir">The target directory of this bot context.</param>
		/// <param name="ctx">The bot context to attach this handler to.</param>
		private XFileHandler(string dir, BotContext ctx = null) {
			CreateEntirePathIfDoesntExist(dir, true);
			BasePath = dir;
			Context = ctx;
		}

		/// <summary>
		/// Returns all of the lines of a file. The specified path should be relative to <see cref="BasePath"/>.
		/// </summary>
		/// <param name="path">The path of the file relative to <see cref="BasePath"/></param>
		/// <returns></returns>
		[Obsolete("Use ReadAllLines instead.")]
		public string[] GetLinesOfFile(string path) {
			if (!File.Exists(Path.Combine(BasePath, path))) {
				File.CreateText(Path.Combine(BasePath, path)).Close();
			}
			return File.ReadAllLines(Path.Combine(BasePath, path));
		}

		/// <summary>
		/// Create a new file for text writing. The specified path should be relative to <see cref="BasePath"/>.
		/// </summary>
		/// <param name="path">The path of the file relative to <see cref="BasePath"/>.</param>
		/// <returns></returns>
		public StreamWriter CreateText(string path) {
			return File.CreateText(Path.Combine(BasePath, path));
		}

		/// <summary>
		/// Opens a file in the path, which should be relative to <see cref="BasePath"/>.
		/// </summary>
		/// <param name="path">The path of the file relative to <see cref="BasePath"/>.</param>
		/// <param name="mode">The optional mode for how to open this file.</param>
		/// <returns></returns>
		public FileStream OpenFile(string path, FileMode mode = FileMode.OpenOrCreate) {
			return File.Open(Path.Combine(BasePath, path), mode);
		}

		/// <summary>
		/// Writes all text to the specified file. The specified path should be relative to <see cref="BasePath"/>.
		/// </summary>
		/// <param name="path">The path of the file relative to <see cref="BasePath"/>.</param>
		/// <param name="contents">The contents of this file.</param>
		public void WriteAllText(string path, string contents) {
			File.WriteAllText(Path.Combine(BasePath, path), contents);
		}

		/// <summary>
		/// Reads all text from the specified file. The specified path should be relative to <see cref="BasePath"/>.
		/// </summary>
		/// <param name="path">The path of the file relative to <see cref="BasePath"/>.</param>
		public string ReadAllText(string path) {
			return File.ReadAllText(Path.Combine(BasePath, path));
		}

		/// <summary>
		/// Returns all of the lines of a file. The specified path should be relative to <see cref="BasePath"/>.
		/// </summary>
		/// <param name="path">The path of the file relative to <see cref="BasePath"/></param>
		/// <returns></returns>
		public string[] ReadAllLines(string path) {
			return File.ReadAllLines(Path.Combine(BasePath, path));
		}

		/// <summary>
		/// Deletes the specified file. The specified path should be relative to <see cref="BasePath"/>.
		/// </summary>
		/// <param name="path">The path of the file relative to <see cref="BasePath"/>.</param>
		public void Delete(string path) {
			File.Delete(Path.Combine(BasePath, path));
		}

		/// <summary>
		/// Returns whether or not the specified file exists. The specified path should be relative to <see cref="BasePath"/>.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public bool Exists(string path) {
			return File.Exists(Path.Combine(BasePath, path));
		}

		/// <summary>
		/// Create a directory.
		/// </summary>
		/// <param name="path">The path of the file relative to <see cref="BasePath"/>.</param>
		/// <returns></returns>
		public DirectoryInfo CreateDirectory(string path) {
			if (!Directory.Exists(Path.Combine(BasePath, path))) {
				return Directory.CreateDirectory(Path.Combine(BasePath, path));
			}
			return new DirectoryInfo(Path.Combine(BasePath, path));
		}

		/// <summary>
		/// Returns an <see cref="XFileHandler"/> targetting the specified <seealso cref="BotContext"/>. This caches the handler internally.<para/>
		/// </summary>
		/// <param name="context">The bot context that this targets. If this is null, it will return <see cref="GLOBAL_HANDLER"/></param>
		/// <returns></returns>
		public static XFileHandler GetFileHandlerForBotContext(BotContext context, string subDir = "") {
			if (context == null) return GLOBAL_HANDLER;
			//if (StoredContextBindings.TryGetValue(context, out XFileHandler handler)) return handler;
			if (StoredContextBindings.ContainsKey(context) && StoredContextBindings[context].ContainsKey(subDir)) {
				return StoredContextBindings[context][subDir];
			}

			if (!StoredContextBindings.ContainsKey(context)) {
				StoredContextBindings[context] = new Dictionary<string, XFileHandler>();
			}

			XFileHandler handler = new XFileHandler(Path.Combine(BotCoreDirectory, context.DataPersistenceName, subDir), context);
			StoredContextBindings[context][subDir] = handler;
			return handler;
		}

		/// <summary>
		/// Creates a new <see cref="XFileHandler"/> targetting a directory inside of <see cref="BotCoreDirectory"/>\customdata\<para/>
		/// This does not cache.
		/// </summary>
		/// <param name="name">The name of the directory to create.</param>
		/// <returns></returns>
		public static XFileHandler CreateNewHandlerInDataDirectory(string name) {
			return new XFileHandler(Path.Combine(BotCoreDirectory, "customdata", name));
		}

		/// <summary>
		/// Creates a new <see cref="XFileHandler"/> in a custom directory.<para/>
		/// This does not cache.
		/// </summary>
		/// <param name="directory">The directory to target.</param>
		/// <returns></returns>
		public static XFileHandler CreateNewHandlerInCustomDirectory(DirectoryInfo directory) {
			return new XFileHandler(directory.FullName);
		}

		/// <summary>
		/// Recursively creates a filepath.
		/// </summary>
		/// <param name="path">The path of the file or folder.</param>
		/// <param name="isFolder">Whether or not the target path is a folder.</param>
		public static void CreateEntirePathIfDoesntExist(string path, bool isFolder = false) {
			if (path.EndsWith("\\")) {
				path = path.Substring(0, path.Length - 1);
			}
			string[] components = path.Split('\\');
			string currentPathSoFar = "";
			foreach (string component in components) {
				currentPathSoFar += component + "\\";
				if (component == components.Last()) {
					CreateIfDoesntExist(currentPathSoFar, isFolder);
				} else {
					CreateIfDoesntExist(currentPathSoFar, true);
				}
			}
		}

		/// <summary>
		/// Creates the specified file or folder if it doesn't exist.
		/// </summary>
		/// <param name="path">The path to this file or folder.</param>
		/// <param name="isFolder">Whether or not this is a folder.</param>
		public static void CreateIfDoesntExist(string path, bool isFolder = false) {
			if (!isFolder) {
				while (path.EndsWith("\\")) {
					path = path.Substring(0, path.Length - 1);
				}
				if (!File.Exists(path)) {
					FileStream newFile = File.Create(path);
					newFile.Close();
				}
			} else {
				if (!Directory.Exists(path)) {
					Directory.CreateDirectory(path);
				}
			}
		}
	}
}
