using System;
using System.IO;
using System.Text;

namespace PM {

    public static class ClientData
    {
        public const string SAVE_FILE_NAME = "multiplayersave";
        const string USER_DATA_FILE_NAME = "userData.txt";

        public static void saveData()
        {
            //save turn data into a file. tick first and then cycle
            string[] userData = new string[]
            {
                    name,
                    code,
                    hostIP,
                    savePath
            };
            File.WriteAllLines(USER_DATA_FILE_NAME, userData);
        }
        public static string name { get; set; }
        public static string code { get; set; }
        public static string hostIP { get; set; }
        public static string savePath { get; set; }
        public static bool connectedToHost { get; set; }
        public static void loadData()
        {
            if (File.Exists(USER_DATA_FILE_NAME))
            {
                var dataAsList = File.ReadAllLines(USER_DATA_FILE_NAME, Encoding.UTF8);
                name = dataAsList[0];
                code = dataAsList[1];
                hostIP = dataAsList[2];
                savePath = dataAsList[3];
            }
            else
            {
                System.Random rnd = new System.Random();

                name = Convert.ToString(rnd.Next(0, 9999));
                code = Convert.ToString(rnd.Next(1000, 9999));
                hostIP = "0.0.0.0";
                savePath = "C:\\Users\\your_windows_username_here\\AppData\\Local\\Colossal Order\\Cities_Skylines\\Saves\\";
                File.WriteAllText(USER_DATA_FILE_NAME, name + "\n" + code + "\n" + hostIP + "\n" + savePath, Encoding.UTF8);
            }
        }
    }

}