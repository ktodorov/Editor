using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows;
using Windows.Storage;
using Windows.Storage.Pickers;
using System.IO;
using RemedyPic;

namespace RemedyPic.Common
{
	class Configuration
    {
        
		public string[] effects = new string[256];
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

		

		#endregion

        #region Export
        public async void Export(List<string> effectsApplied)
        {
            
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add(".TXT", new List<string>() { ".txt" });
            StorageFile file = await savePicker.PickSaveFileAsync();       
            string EffectsForWrite = "";

            if (file != null)
            // File is null if user cancels the file picker.
            {
                for (int i = 0; i < effectsApplied.Count; i++)
                {
                    EffectsForWrite += effectsApplied[i] + "\n";
                    
                }
                await Windows.Storage.FileIO.WriteTextAsync(file, EffectsForWrite);
            }       
        }
        #endregion

        #region Import
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


				string[] contents = new string[250];
				contents[0] = rdr.ReadLine();

				for (int i = 0; i < 1; i++)
				{
					applyEffects(contents[i]);
				}

			}

		}

		private void applyEffects(string applies)
		{
			string[] words = applies.Split(' ');
			if (words.Length == 3)
			{
				string firstWord = words[0];
				string value = words[2];

				apply(firstWord, value);
			}

		}

		private void apply(string effect, string value)
		{
			switch (effect)
			{
				case "filter":
					applyFilter(value);
					break;
				default:
					break;
			}
		}

		private void applyFilter(string value)
		{
			switch (value)
			{
				case "blackwhite":
					effects[0] = "blackwhite";
					break;
				default:
					break;
			}
		}

        #endregion

    }
}
