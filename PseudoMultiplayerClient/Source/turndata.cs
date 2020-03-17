using System;
using System.IO;
using System.Text;

namespace PM {
    public static class TurnData
    {
        const string TURN_DATA_FILE_NAME = "turnData";
        public static void saveTurnData()
        {
            //save turn data into a file. tick first and then cycle
            string[] turnData = new string[]
            {
                    tick.ToString(),
                    cycle.ToString()
            };
            File.WriteAllLines(TURN_DATA_FILE_NAME, turnData);
        }
        public static uint cycle { get; set; }
        public static uint tick { get; set; }
        public static void nullifyTurnData()
        {
            tick = 0;
            cycle = 0;
            saveTurnData();
        }
        public static void loadTurnData()
        {
            if (File.Exists(TURN_DATA_FILE_NAME))
            {
                string[] dataAsList = File.ReadAllLines(TURN_DATA_FILE_NAME, Encoding.UTF8);
                if (dataAsList.Length != 2)
                {
                    //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "loadTurnData(): wrong amount of lines in a turnData file (" + dataAsList.Length + "). Nullifying said file.");
                    nullifyTurnData();
                }
                tick = Convert.ToUInt32(dataAsList[0]);
                cycle = Convert.ToUInt32(dataAsList[1]);
            }
            else
            {
                nullifyTurnData();
                //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "WARNING: loadTurnData() was called while the file did not exist!\n");
            }
        }
    }

}