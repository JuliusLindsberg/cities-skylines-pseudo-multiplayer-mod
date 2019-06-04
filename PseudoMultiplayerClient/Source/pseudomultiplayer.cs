using System;
using ICities;
using System.Net.Sockets;
using System.IO;
using System.Text;
using PMCommunication;
using System.Timers;

namespace PM
{
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
                if(dataAsList.Length != 2)
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
                hostIP
            };
            File.WriteAllLines(USER_DATA_FILE_NAME, userData);
        }
        public static string name { get; set; }
        public static string code { get; set; }
        public static string hostIP { get; set; }
        public static bool connectedToHost { get; set; }
        public static void loadData()
        {
            if (File.Exists(USER_DATA_FILE_NAME))
            {
                var dataAsList = File.ReadAllLines(USER_DATA_FILE_NAME, Encoding.UTF8);
                name = dataAsList[0];
                code = dataAsList[1];
                hostIP = dataAsList[2];
            }
            else
            {
                System.Random rnd = new System.Random();
                
                name = Convert.ToString(rnd.Next(0, 9999));
                code = Convert.ToString(rnd.Next(1000, 9999));
                File.WriteAllText(USER_DATA_FILE_NAME, name + "\n" + code + "\n" + "0.0.0.0", Encoding.UTF8);
                hostIP = "0.0.0.0";
            }
        }
    }

    public class PseudoMultiplayer : IUserMod
    {
        //this variable here should find some other place eventually
        public static ISerializableData PMSerializingManager;

        public string Name
        {
            get { return "The amazing pseudo multiplayer mod in cites: skylines!"; }
        }

        public string Description
        {
            get { return "This mod aims to make a kind of multiplayer experience of cities skylines with players taking turns on city management with the help of a dedicated server."; }
        }
        public void OnSettingsUI(UIHelperBase helper)
        {
            string connectionStatus = "(NOT CONNECTED)";
            if (ClientData.connectedToHost)
            {
                connectionStatus = "(CONNECTED)";
            }
            UIHelperBase group = helper.AddGroup("Pseudo_Multiplayer_group");
            ClientData.loadData();
            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "ClientData Loaded! name: " + ClientData.name + ", code: " + ClientData.code + ", ip: " + ClientData.hostIP + "!");
            group.AddTextfield("Host ip address", ClientData.hostIP, (value) => updateClientDataFromUI(value, "ip"));
            group.AddTextfield("Username", ClientData.name, (value) => updateClientDataFromUI(value, "name"));
            group.AddTextfield("User code (not encrypted, don't use the same code anywhere else!!!)", ClientData.code, (value) => updateClientDataFromUI(value, "code"));
            group.AddButton("Attempt join: " + connectionStatus, () => attemptToJoinServer(group));
        }
        public void updateClientDataFromUI(string value, string identifier)
        {
            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "updateClientDataFromUI()!");
            if( identifier == "ip" )
            {
                ClientData.hostIP = value;
            }
            else if (identifier == "name")
            {
                ClientData.name = value;
            }
            else if( identifier == "code" )
            {
                ClientData.code = value;
            }
            else
            {
                //DebugOutputPanel.AddMessage(PluginManager.MessageType.Error, "bad identifier(" + identifier + ")inserted into function updateClientFromUI()!");
            }
            ClientData.saveData();
        }
        public void attemptToJoinServer(UIHelperBase group)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(ClientData.hostIP, Message.PORT);
            byte[] message = new Message(ClientData.name, ClientData.code, MessageStrings.joinGame).messageAsByteArray();
            socket.Send(message);

            byte[] buffer = new byte[1];
            socket.Receive(buffer);
            if(buffer[0] == (byte)Responses.JoinAccepted)
            {
                //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "join accepted!");
                ClientData.connectedToHost = true;
                group.AddTextfield("Connection Status:", "ACCEPTED!", (value) => doNothing());
            }
            else
            {
                //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "join refused!");
                ClientData.connectedToHost = false;
            }
        }
        //send essential data to server in this funtion(the most current saveGame and maybe in future some statistics collected)
        public static void sendSaveToServer(Object source, ElapsedEventArgs eventArgs)
        {
            string pathAndFileName = "C:\\Users\\Mooncat\\AppData\\Local\\Colossal Order\\Cities_Skylines\\Saves\\" + ClientData.SAVE_FILE_NAME+ "_sent.crp";
            if (!File.Exists(pathAndFileName))
            {
                //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "File" + pathAndFileName + " does not exist!");
                return;
            }
            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "SendDataToServer()");
            var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(ClientData.hostIP, Message.PORT);
            ClientData.loadData();
            clientSocket.Send(new Message(ClientData.name, ClientData.code, MessageStrings.saveToHost).messageAsByteArray());
            clientSocket.SendFile(pathAndFileName);
            clientSocket.Close();
            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Save file sent now");
            TurnData.nullifyTurnData();
            return;
        }
        public static void receiveSaveFromServer()
        {
            var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(ClientData.hostIP, Message.PORT);
            var message = new PMCommunication.Message(ClientData.name, ClientData.code, PMCommunication.MessageStrings.saveToClient);
            clientSocket.Send(message.messageAsByteArray());
            using (FileStream output = File.Create("C:\\Users\\Mooncat\\AppData\\Local\\Colossal Order\\Cities_Skylines\\Saves\\" + ClientData.SAVE_FILE_NAME+"_received.crp"))
            {
                // read the file in chunks of 1KB
                byte[] buffer = new byte[1024];
                int bytesRead;
                while ((bytesRead = clientSocket.Receive(buffer)) > 0)
                {
                    output.Write(buffer, 0, bytesRead);
                }
            }
        }
        public void doNothing()
        {

        }
    }
    public class PMSerializationManager : SerializableDataExtensionBase
    {
        static public bool multiplayerTurnEndSave;
        ISerializableData managerObject;
        public override void OnCreated(ISerializableData serializedData)
        {
            multiplayerTurnEndSave = false;
            managerObject = serializedData;
            PM.PseudoMultiplayer.PMSerializingManager = serializedData;
        }

        public override void OnLoadData()
        {

        }

        public override void OnReleased()
        {

        }

        public override void OnSaveData()
        {
            if (PMThreading.PMActive || multiplayerTurnEndSave)
            {
                PM.TurnData.saveTurnData();
            }
            //OnSaveData() is called after the vanilla game has saved. Therefore it is the best place to put this timer that waits for 5 seconds and then tries to send the save to server
            if (multiplayerTurnEndSave)
            {
                var saveTimer = new System.Timers.Timer(15000);
                saveTimer.Elapsed += PseudoMultiplayer.sendSaveToServer;
                saveTimer.AutoReset = false;
                saveTimer.Start();
                multiplayerTurnEndSave = false;
            }
            
        }
    }
    public class PMLoader : LoadingExtensionBase
    {
        public override void OnCreated(ILoading loading)
        {
            //asks the server about who's turn is it right now
            ClientData.loadData();
            if (ClientData.hostIP != "0.0.0.0") {
                HostConfigData.fetchRemoteData(ClientData.name, ClientData.code, ClientData.hostIP);
                if(HostConfigData.playerTurnName == ClientData.name)
                {
                    
                }
            }
        }
        public override void OnLevelLoaded(LoadMode mode)
        {
            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "OnLevelLoaded()");
            TurnData.loadTurnData();
            ClientData.loadData();
            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, ClientData.hostIP + "turnDuration: " + HostConfigData.turnDuration + " turn: " +HostConfigData.turn + " player name: " + HostConfigData.playerTurnName );
            if(ClientData.hostIP == "0.0.0.0")
            {
                PMThreading.PMActive = false;
                return;
            }
            HostConfigData.fetchRemoteData(ClientData.name, ClientData.code, ClientData.hostIP);
            if(HostConfigData.playerTurnName == ClientData.name || HostConfigData.turn == 0)
            {
                //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "PMActive == true");
                PMThreading.PMActive = true;
            }
            else
            {
                //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "PMActive == false");
                PMThreading.PMActive = false;
            }

        }
        
    }

    public class PMThreading : ICities.ThreadingExtensionBase
    {
        IThreading PMthreadingManager;
        public static bool PMActive = false;
        public override void OnCreated(IThreading threading)
        {
            PMthreadingManager = threading;
        }
        public override void OnAfterSimulationFrame()
        {
            if (!PMActive)
            {
                return;
            }
            TurnData.tick++;
            if (TurnData.tick >= HostConfigData.cycleDuration)
            {
                TurnData.tick = 0;
                TurnData.cycle++;
                
            }
            if (TurnData.cycle >= HostConfigData.turnDuration)
            {
                //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "This clients turn has ended.");
                PM.PMSerializationManager.multiplayerTurnEndSave = true;
                PseudoMultiplayer.PMSerializingManager.SaveGame(ClientData.SAVE_FILE_NAME+"_sent");
                PMActive = false;
            }
            
        }
    }

}