using System;
using ICities;
using System.Net.Sockets;
using System.IO;
using System.Text;
using PMCommunication;
using System.Timers;

namespace PM
{
    public class PseudoMultiplayer : IUserMod
    {
        //this variable here should find some other place eventually
        public static ISerializableData PMSerializingManager;

        public string Name
        {
            get { return "Pseudo multiplayer mod"; }
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
            group.AddTextfield("User code (not encrypted anywhere, don't use the same code anywhere else!)", ClientData.code, (value) => updateClientDataFromUI(value, "code"));
            group.AddTextfield("File path ( the location of your save files. Insert your windows username)",
                ClientData.savePath, (value) => updateClientDataFromUI(value, "savePath"));
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
            else if ( identifier == "savePath")
            {
                ClientData.savePath = value;
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
            using (FileStream output = File.Create("C:\\Users\\Mooncat\\AppData\\Local\\Colossal Order\\Cities_Skylines\\Saves\\" + ClientData.SAVE_FILE_NAME + "_received.crp"))
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
}