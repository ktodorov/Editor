﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows;
using Windows.Storage;
using Windows.Storage.Pickers;
using System.IO;

namespace RemedyPic.Common
{
	class Configuration
	{
		#region Getters and setters.
		public StorageFile configFile
		{
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

		public async void Import()
		{
			FileOpenPicker filePicker = new FileOpenPicker();
			filePicker.FileTypeFilter.Add(".txt");
			StorageFile file = await filePicker.PickSingleFileAsync();

			if (file != null)
			// File is null if user cancels the file picker.
			{

				var stream = await file.OpenReadAsync();
				var rdr = new StreamReader(stream.AsStream());

				var contents = rdr.ReadToEnd();
			}


		}

	}
}
