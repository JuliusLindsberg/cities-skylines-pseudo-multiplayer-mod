using System;
using ICities;
using System.Net.Sockets;
using System.IO;
using System.Text;
using PMCommunication;
using System.Timers;
using ColossalFramework.UI;
using System.Collections;

namespace PM
{
    public class PseudoMultiplayer : IUserMod
    {
        //this variable here should find some other place eventually
        public static ISerializableData PMSerializingManager;
        private UIView view = UIView.GetAView();

        public string Name
        {
            get { return "Pseudo multiplayer mod"; }
        }

        public string Description
        {
            get { return "This mod aims to make a kind of multiplayer experience of cities skylines with players taking turns on city management with the help of a seperate dedicated server."; }
        }
        public void OnSettingsUI(UIHelperBase helper)
        {
            string connectionStatus = "(NOT JOINED)";
            if (ClientData.connectedToHost)
            {
                connectionStatus = "(JOINED)";
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
            group.AddButton("Is it my turn now?", () => askTurn(group));
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
        public void askTurn(UIHelperBase group)
        {
            ClientData.loadData();
            TurnData.loadTurnData();
            try
            {
                HostConfigData.fetchRemoteData(ClientData.name, ClientData.code, ClientData.hostIP);
            }
            catch(Exception e)
            {
                group.AddTextfield("No Response from server!", e.Message, (value) => doNothing());
                ClientData.connectedToHost = false;
                ClientData.saveData();
                return;
            }
            if (HostConfigData.playerTurnName == ClientData.name) {
                if (HostConfigData.playerTurnName == ClientData.name && !TurnData.saveFetched)
                {
                    PseudoMultiplayer.receiveSaveFromServer();
                    group.AddTextfield("Response from server: ", "Yes it is. An up to date save was retrieved from host into save name: " + ClientData.SAVE_FILE_NAME + "_received.crp", (value) => doNothing());
                }
                else
                {
                    group.AddTextfield("Response from server: ", "Yes it is.", (value) => doNothing());
                }
            }
            else if (HostConfigData.playerTurnName == "First turn")
            {
                group.AddTextfield("Response from server:", "As it is the first turn it is up to the players to decide.", (value) => doNothing());
            }
            else
            {
                group.AddTextfield("Response from server: ", "Nope. It is player " + HostConfigData.playerTurnName + "'s turn.", (value) => doNothing());
            }
        }
        public void attemptToJoinServer(UIHelperBase group)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            byte[] message = new Message(ClientData.name, ClientData.code, MessageStrings.joinGame).messageAsByteArray();
            byte[] buffer = new byte[1];
            try
            {
                socket.Connect(ClientData.hostIP, Message.PORT);
                socket.Send(message);
                socket.Receive(buffer);
            }
            catch(Exception e)
            {
                group.AddTextfield("Join game status:", e.Message, (value) => doNothing());
            }
            if(buffer[0] == (byte)Responses.JoinAccepted)
            {
                //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "join accepted!");
                ClientData.connectedToHost = true;
                ClientData.saveData();
                group.AddTextfield("Join game status:", "ACCEPTED!", (value) => doNothing());
            }
            else if(buffer[0] == (byte)Responses.AlreadyJoined)
            {
                group.AddTextfield("Join game status:", "ALREADY JOINED", (value) => doNothing());
            }
            else if(buffer[0] == (byte)Responses.NameTaken)
            {
                ClientData.connectedToHost = false;
                group.AddTextfield("Join game status:", "Your username in this game is already taken", (value) => doNothing());
            }
            else if(buffer[0] == (byte)Responses.JoinRefused)
            {
                group.AddTextfield("Join game status:", "Join refused: is the game already running?", (value) => doNothing());
            }
            else
            {
                group.AddTextfield("Join game status:", "Unknown error", (value) => doNothing());
            }
        }
        //send essential data to server in this funtion(the most current saveGame and maybe in future some statistics collected)
        public static void sendSaveToServer(Object source, ElapsedEventArgs eventArgs)
        {
            string pathAndFileName = ClientData.savePath + ClientData.SAVE_FILE_NAME + ".crp";
            if (!File.Exists(pathAndFileName))
            {
                DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Error, " Savefile did not exist!");
                throw new FileNotFoundException("File " + pathAndFileName + " was not found!" );
            }
            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "SendDataToServer()");
            byte[] responseByte = new byte[1];
            var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(ClientData.hostIP, Message.PORT);
            clientSocket.Send(new Message(ClientData.name, ClientData.code, MessageStrings.saveToHost).messageAsByteArray());
            clientSocket.Receive(responseByte);
            if(responseByte[0] != (byte)PMCommunication.Responses.ReceivingSave)
            {
                throw new Exception("Host was not willing to receive a savefile for unknown reason");
            }
            clientSocket.SendFile(pathAndFileName);
            clientSocket.Close();
            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Save file sent now");
            TurnData.nullifyTurnData();
        }

        public static void receiveSaveFromServer()
        {
            var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(ClientData.hostIP, Message.PORT);
            var message = new PMCommunication.Message(ClientData.name, ClientData.code, PMCommunication.MessageStrings.saveToClient);
            clientSocket.Send(message.messageAsByteArray());
            using (FileStream output = File.Create(ClientData.savePath + ClientData.SAVE_FILE_NAME + "_received.crp"))
            {
                // read the file in chunks of 1KB
                byte[] buffer = new byte[1024];
                int bytesRead;
                while ((bytesRead = clientSocket.Receive(buffer)) > 0)
                {
                    output.Write(buffer, 0, bytesRead);
                }
            }
            TurnData.cycle = 0;
            TurnData.tick = 0;
            TurnData.saveFetched = true;
            TurnData.saveTurnData();
            //view.AddUIComponent();
            return;
        }
        public void doNothing()
        {

        }
    }

}