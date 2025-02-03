using System;
using System.IO;
using UnityEngine;

public class SaveLoadSystem
{
    public class Local
    {
        static readonly string fileFormat = ".json";

        static string GetFilePath(string folderName, string fileName)
        {
            return $"{Application.persistentDataPath}/{folderName}/{fileName + fileFormat}";
        }

        public class PlayerSettings
        {
            static readonly string folderName = "PlayerSettings";
            static readonly string fileName = "PlayerSettings";

            static string GetPath() => GetFilePath(folderName, fileName);

            public static void Load()
            {
                string filePath = GetPath();

                bool isSaveFileExists = CheckSaveGame(filePath);

                if (!isSaveFileExists)
                    Save(true);

                global::PlayerSettings playerSettings = Load<global::PlayerSettings>(filePath);

                GameManager.instance.PLAYER_SETTINGS = playerSettings;

                LanguageManager.instance.SetUserLanguage();
            }

            public static void Save(bool setDefault = false)
            {
                string filePath = GetPath();

                global::PlayerSettings playerSettings = setDefault ? new() : GameManager.instance.PLAYER_SETTINGS;

                Save<global::PlayerSettings>(playerSettings, filePath);
            }
        }

        /// <returns>True if the file is exists in the given <c>filePath</c>.</returns>
        static bool CheckSaveGame(string filePath)
        {
            if (File.Exists(filePath))
                return true;

            return false;
        }

        /// <summary>
        /// Loads the given type of data from the <c>filePath</c>.
        /// </summary>
        static T Load<T>(string filePath)
        {
            T loadedData = default;

            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);

                    json = SaveLoadUtils.DecryptData(json);

                    loadedData = JsonUtility.FromJson<T>(json);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error occured when trying to load data from file: " + "\n" + e);
                }
            }

            return loadedData;
        }

        /// <summary>
        /// Saves the given type of data to the given <c>filePath</c>.
        /// </summary>
        static void Save<T>(T dataToSave, string filePath)
        {
            string directoryPath = Path.GetDirectoryName(filePath);

            try
            {
                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                string dataToStore = JsonUtility.ToJson(dataToSave, false);

                dataToStore = SaveLoadUtils.EncryptData(dataToStore);

                File.WriteAllText(filePath, dataToStore);
            }
            catch (Exception e)
            {
                Debug.LogError("Error occured when trying to save data to file: " + "\n" + e);
            }
        }
    }
}
