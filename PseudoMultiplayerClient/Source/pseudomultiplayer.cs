using System;
using ICities;
using System.Net.Sockets;
using System.Net;
using ColossalFramework.Plugins;
using System.IO;
using System.Text;
using PMCommunication;

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
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "HIPS");
                string[] dataAsList = File.ReadAllLines(TURN_DATA_FILE_NAME, Encoding.UTF8);
                if(dataAsList.Length != 2)
                {
                    DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "loadTurnData(): wrong amount of lines in a turnData file (" + dataAsList.Length + "). Nullifying said file.");
                    nullifyTurnData();
                }
                tick = Convert.ToUInt32(dataAsList[0]);
                cycle = Convert.ToUInt32(dataAsList[1]);
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "HOPS");
            }
            else
            {
                nullifyTurnData();
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "WARNING: loadTurnData() was called while the file did not exist!\n");
            }
        }
    }
    public static class ClientData
    {
        public const string SAVE_FILE_NAME = "multiplayersave";
        const string USER_DATA_FILE_NAME = "userData.txt";

        public static void saveData()
        {
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "SAVING DATA!");
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
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "in loadData(): FILE EXISTS!");
                var dataAsList = File.ReadAllLines(USER_DATA_FILE_NAME, Encoding.UTF8);
                name = dataAsList[0];
                code = dataAsList[1];
                hostIP = dataAsList[2];
            }
            else
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "in loadData(): CREATING NEW FILE!");
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
            group.AddButton("Attempt join: " + connectionStatus, () => attemptJoinServer(group));
        }
        public void updateClientDataFromUI(string value, string identifier)
        {
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "updateClientDataFromUI()!");
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
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Error, "bad identifier(" + identifier + ")inserted into function updateClientFromUI()!");
            }
            ClientData.saveData();
        }
        public void attemptJoinServer(UIHelperBase group)
        {
            try
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(ClientData.hostIP, Message.PORT);
                byte[] message = new Message(ClientData.name, ClientData.code, MessageStrings.joinGame).messageAsByteArray();
                socket.Send(message);

                byte[] buffer = new byte[1];
                socket.Receive(buffer);
                if(buffer[0] == (byte)Responses.JoinAccepted)
                {
                    DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "join accepted!");
                    ClientData.connectedToHost = true;
                    group.AddTextfield("Connection Status:", "ACCEPTED!", (value) => doNothing());
                }
                else
                {
                    DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "join refused!");
                    ClientData.connectedToHost = false;
                }
            }
            catch
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "failed connection!");
                ClientData.connectedToHost = false;
            }
        }
        //send essential data to server in this funtion(the most current saveGame and maybe in future some statistics collected)
        public static bool sendSaveDataToServer(string pathAndFileName, int port = 2556)
        {
            
            if(!File.Exists(pathAndFileName))
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "File" + pathAndFileName + " does not exist!");
                return false;
            }
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "SendDataToServer()");
            /*Byte[] addressAsBytes = { 0, 0, 0, 0 };
            IPAddress ipAddr = new IPAddress(addressAsBytes);
            IPEndPoint endPoint = new IPEndPoint(IP, port);*/
            //SocketInformation info = new SocketInformation();

            var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //try
            //{
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "A");
                clientSocket.Connect(ClientData.hostIP, Message.PORT);
                //clientSocket.Connect(endPoint);
                PM.ClientData.loadData();
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "B");


                //for now lets just hope this conversion won't cause problems... it shouldn't really as long name or code lengths aren't going to be supported anyhow
                byte nameLength = (byte)Encoding.UTF8.GetByteCount(ClientData.name);
                byte codeLength = (byte)Encoding.UTF8.GetByteCount(ClientData.code);
                byte messageLength = (byte)Encoding.UTF8.GetByteCount(MessageStrings.saveToHost);
                byte[] message = new byte[nameLength + codeLength + messageLength + 3];
                //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "C");
                message[0] = codeLength;
                message[1] = nameLength;
                message[2] = messageLength;
                //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "D");
                Array.Copy(Encoding.UTF8.GetBytes(ClientData.code + ClientData.name + MessageStrings.saveToHost), 0, message, 3, ClientData.code.Length + ClientData.name.Length + 1);

                clientSocket.Send(new Message(ClientData.name, ClientData.code, MessageStrings.saveToHost).messageAsByteArray());
                //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "E");
                //byte[] serverReply = new byte[1000];
                //serverReply[0] = (byte)Responses.ReceivingSave;
                //clientSocket.Receive(serverReply);
                /*if (serverReply[0] != (int)Responses.ReceivingSave)
                {
                    DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "request to send the save file was denied by host");
                    return false;
                }
                else
                {*/
                    //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "save send request is accepted");
                //}
                //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "F");
                clientSocket.SendFile(pathAndFileName);
                clientSocket.Close();
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Save file sent now");
            //}
            /*catch (Exception e)
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Pseudomultiplayer mod was unable to contact the client! You may try to connect server by loading the save again.\n (" + e.ToString() + ")");
                return false;
            }*/
            //clientSocket.Shutdown(SocketShutdown.Both);
            //clientSocket.Close();
            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "SendDataToServer() END");
            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "save file was sent to host successfully");
            return true;
        }
        public void doNothing()
        {

        }
    }
    public class PMSerializationManager : SerializableDataExtensionBase
    {
        ISerializableData managerObject;
        public override void OnCreated(ISerializableData serializedData)
        {
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
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "saving turn data!");
            PM.TurnData.saveTurnData();

        }
    }
    public class PMLoader : LoadingExtensionBase
    {
        public override void OnCreated(ILoading loading)
        {
            //asks the dedicated server about who's turn is it right now

        }
        public override void OnLevelLoaded(LoadMode mode)
        {
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "OnLevelLoaded()");
            TurnData.loadTurnData();
            ClientData.loadData();
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "OnLevelLoaded(): fetching remote data");
            HostConfigData.fetchRemoteData(ClientData.name, ClientData.code, ClientData.hostIP);
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "OnLevelLoaded(): remote data fetched");
            if(HostConfigData.playerTurnName == ClientData.name || HostConfigData.turn == 0)
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "PMActive == true");
                PMThreading.PMActive = true;
            }
            else
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "PMActive == false");
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
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "This clients turn has ended. Data is being sent to server!");
                //saving the game does not seem to work in this context. Will investigate later on...
                try
                {
                    PseudoMultiplayer.PMSerializingManager.SaveGame(ClientData.SAVE_FILE_NAME);
                }
                catch(Exception e)
                {
                    DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, e.ToString());
                    PMActive = false;
                    return;
                }
                try
                {
                    //NOTE: THIS FUNCTION CALL IS HERE FOR DEBUGGING PURPOSES ONLY!
                    TurnData.nullifyTurnData();
                    //NOTE: THIS ASSIGNMENT OPERATION IS HERE FOR DEBUGGING PURPOSES ONLY!
                    PMActive = false;
                    ///NOTE TO SELF: CLEAR THIS MESS OVER HERE NEXT TIME!
                    ///also add a timer from unityengine (WaitForSeconds()?) here so the game will be able to actually save the game before sending over the save to the server.
                    ///Since SaveGame() appears not to be blocking I am unaware of a better way to implement this yet.
                    if (!PseudoMultiplayer.sendSaveDataToServer("C:\\Users\\Mooncat\\AppData\\Local\\Colossal Order\\Cities_Skylines\\Saves\\" + ClientData.SAVE_FILE_NAME+".crp"))
                    {
                        PMActive = false;
                        //NOTE: THIS FUNCTION CALL IS HERE FOR DEBUGGING PURPOSES ONLY!
                        TurnData.nullifyTurnData();
                        return;
                    }
                }
                catch (Exception e)
                {
                    DebugOutputPanel.AddMessage(PluginManager.MessageType.Error, "Error: was host's IP inserted by user sensible?\n" + e.ToString());
                    PMActive = false;
                    return;
                }

                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Save successful, nullifying turn data");
                //Here a confirmation should be made that the save has also been successfully sent to the server before nullifying turnData
                TurnData.nullifyTurnData();
                PMActive = false;
            }
        }
    }

}