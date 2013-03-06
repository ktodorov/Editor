using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows;
using Windows.Storage;

namespace RemedyPic.Common
{
	class Configuration
	{
		public StorageFile configFile
		{

			#region Getters and setters.
			get
			{
				return configFile;
			}
			set
			{
				configFile = value;
			}
		}

		public string[] effects
		{
			get
			{
				return effects;
			}
			set
			{
				effects = value;
			}
		}
			#endregion



	}
}
