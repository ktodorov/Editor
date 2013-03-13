using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;
using System.IO;
using RemedyPic;

namespace RemedyPic.Common
{
    class Configuration
    {

        public List<string> effects = new List<string>();

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
                    EffectsForWrite += effectsApplied[i] + "\r\n";
                }
                await Windows.Storage.FileIO.WriteTextAsync(file, EffectsForWrite);
            }
        }
        #endregion

        #region Import
        public async void Import(TextBlock givenBlock)
        {
            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.FileTypeFilter.Add(".txt");
            StorageFile file = await filePicker.PickSingleFileAsync();

            if (file != null)
            // File is null if user cancels the file picker.
            {
				givenBlock.Text = file.Name;
                var stream = await file.OpenReadAsync();
                var rdr = new StreamReader(stream.AsStream());

                List<string> contents = new List<string>();

                string temp = rdr.ReadLine();
                do
                {
                    contents.Add(temp);
                    temp = rdr.ReadLine();
                } while (temp != null);

                for (int i = 0; i < contents.Count; i++)
                {
                    applyEffects(contents[i]);
                }

            }

        }

        private void applyEffects(string applies)
        {
            string[] words = applies.Split(' ');
            string effect = words[0];
            string value = words[2];

            if (words.Length > 3)
            {
                for (int i = 3; i < words.Length; i++)
                {
                    value += " " + words[i];
                }
            }
            apply(effect, value);

        }

        private void apply(string effect, string value)
        {
            if (effect != "Filter" && effect != "Color" && effect != "Contrast" && effect != "Exposure"
                && effect != "Colorize" && effect != "Flip" && effect != "Frame")
                return;

            effects.Add(effect);
            effects.Add(value);
        }

        #endregion

    }
}
