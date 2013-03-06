using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows;
using Windows.Storage;
using Windows.Storage.Pickers;


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

        public async void Export()
        {
            FileSavePicker savePicker = new FileSavePicker();

            // Set My Documents folder as suggested location if no past selected folder is available
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add(".TXT", new List<string>() { ".txt" });

            // Default file name if the user does not type one in or select a file to replace

            StorageFile file = await savePicker.PickSaveFileAsync();

            


        }


	}
}
