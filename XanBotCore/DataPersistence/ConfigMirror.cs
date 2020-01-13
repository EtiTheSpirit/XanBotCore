using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XanBotCore.ServerRepresentation;

namespace XanBotCore.DataPersistence {

	/// <summary>
	/// Mirrors instances of <see cref="XConfiguration"/> to a different location.
	/// </summary>
	public class ConfigMirror {

		/// <summary>
		/// The source <see cref="XConfiguration"/> that this is mirroring.
		/// </summary>
		public XConfiguration SourceConfig { get; }

		/// <summary>
		/// The <see cref="XFileHandler"/> that this mirror is using to write to the filesystem.
		/// </summary>
		public XFileHandler ThisHandler { get; }

		/// <summary>
		/// Mirrors the specified <see cref="XConfiguration"/> so that it saves under <paramref name="handler"/>.
		/// </summary>
		/// <param name="source">The source <see cref="XConfiguration"/></param>
		/// <param name="dataPersistenceFolder">The folder to treat like an alternate data persistence folder.</param>
		public ConfigMirror(XConfiguration source, XFileHandler handler) {
			SourceConfig = source;
			ThisHandler = handler;
			source.OnConfigValueChanged += OnConfigValueChanged;
		}

		private void OnConfigValueChanged(BotContext context, string key, string oldValue, string newValue, bool valueJustCreated) {
			MirrorNow();
		}

		/// <summary>
		/// Causes this mirror to copy all contents of the current base <see cref="XConfiguration"/> in its current state to the mirror file destination.
		/// </summary>
		public void MirrorNow() {
			string baseName = "\\GlobalStorageContext\\";
			if (SourceConfig.Context != null)
				baseName = SourceConfig.Context.DataPersistenceName;
			
			string filePath = baseName + @"\" + SourceConfig.ConfigFileName;
			string oldCfgText = SourceConfig.BaseHandler.ReadAllText(SourceConfig.ConfigFileName);

			//Console.WriteLine(ThisHandler.BasePath + filePath);
			XFileHandler.CreateEntirePathIfDoesntExist(Path.Combine(ThisHandler.BasePath, filePath));
			ThisHandler.WriteAllText(filePath, oldCfgText);
		}
	}
}
