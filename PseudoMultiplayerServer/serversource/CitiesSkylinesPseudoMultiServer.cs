using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using PMCommunication;
using System.Text;

namespace PMServer
{
    struct Player
    {
        public string name;
        public string code;
        public Player(string _name, string _code)
        {
            name = _name; code = _code;
        }
    }

    class Host
    {
        const string SAVE_FILE_NAME = "multiplayerSave";
        const string PLAYER_LIST_FILE_NAME = "playerList.txt";
        //the string is players name(visible to others) and the int is for login code(not visible, but not encrypted while transmitting it in any way either)
        //ideally, a player should be able to stay oblivious about his own login code, but it should also be possible to check or change in-game via cities skylines mod options
        public Host()
        {
        }
        public void handleConnections()
        {
            Console.WriteLine("Opening the connections!");
            TcpListener listener = new TcpListener(Message.PORT);
            listener.Start();
            while (true)
            {
                using (var client = listener.AcceptSocket())
                {
                    Console.WriteLine("Client connected. Reading the client's request!");
                    byte[] messageBuffer = new byte[1024];
                    //stream.Read(messageBuffer, 0, messageBuffer.Length);
                    client.Receive(messageBuffer);
                    Message message = new Message();
                    if(!message.parseFromString(messageBuffer))
                    {
                        client.Close();
                        Console.WriteLine("Message from the client contained mostly hogwash and heresay. Closing connetion to client.");
                        continue;
                    }
                    // at least for now the first byte in the reply represents the message in a nutshell. if it is 0 or 1, no reply message will be sent at all. 0 will close connection to client immediately, 
                    //and 1 will start to receive a savegame file from a client.
                    byte[] reply = reactToMessage(message);
                    //if the message doesn't contain anything sensible or either the player or code information is wrong, close the tcp connection immediately
                    //might want to send error messages as replies in the future. I'll need to look into what the default behaviour for closed connections looks like in-game.
                    if(reply[0] == (byte)Responses.ConnectionRejected)
                    {
                        //apparently 'using' will cause the connection to be closed anyways.
                        //client.Close();
                        Console.WriteLine("Invalid operation was requested by the server. Connection with client is closed.");
                    }
                    else if (reply[0] == (byte)Responses.ReceivingSave)
                    {
                        using (var output = File.Create(SAVE_FILE_NAME))
                        {
                            Console.WriteLine("A");
                            // read the file in chunks of 1KB
                            byte[] buffer = new byte[1024];
                            int bytesRead;
                            while ((bytesRead = client.Receive(buffer)) > 0)
                            {
                                output.Write(buffer, 0, bytesRead);
                            }
                        }
                        Console.WriteLine("Savefile received!");
                    }
                    else if (reply[0] == (byte)Responses.SendingSave)
                    {
                        client.SendFile(SAVE_FILE_NAME);
                    }
                    else
                    {
                        Console.WriteLine("Sending a reply: ", reply, "\n");
                        //stream.Write(reply, 0, reply.Length);
                        client.Send(reply, reply.Length, SocketFlags.None);
                        Console.WriteLine("The client received the reply successfully.");
                        //client.Close();
                    }
                }
            }
        }
        byte[] reactToMessage(Message message)
        {
            var response = new byte[1];
            List<Player> players = peekPlayers();
            if (message.message == MessageStrings.joinGame)
            {
                if (PMCommunication.HostConfigData.turn > 0)
                {
                    response[0] = (byte)Responses.JoinRefused;
                    return response;
                }
                for (var i = 0; i < players.Count; i++)
                {
                    if (players[i].name == message.name)
                    {
                        response[0] = (byte)Responses.NameTaken;
                        return response;
                    }
                }
                addPlayer(message.name, message.code);
                response[0] = (byte)Responses.JoinAccepted;
                return response;
            }
            //every other command than join requires a player identification. Therefore there needs to be players in the first place for the commands
            if (players == null)
            {
                Console.WriteLine("players == null, ConnectionRejected");
                response[0] = (byte)Responses.ConnectionRejected;
                return response;
            }
            for(var i = 0; i < players.Count; i++)
            {
                if (message.code == players[i].code && message.name == players[i].name)
                {
                    if (message.message == MessageStrings.saveToHost)
                    {
                        Console.WriteLine("message: save, Receiving save");
                        response[0] = (byte)Responses.ReceivingSave;
                        return response;
                    }
                    else if (message.message == MessageStrings.saveToClient)
                    {
                        if(message.name == players[(int)HostConfigData.playerTurn].name)
                        {
                            if (message.code == players[(int)HostConfigData.playerTurn].code)
                            {
                                response[0] = (byte)Responses.SendingSave;
                            }
                            else
                            {
                                response[0] = (byte)Responses.WrongCode;
                            }
                        }
                        else {
                            response[0] = (byte)Responses.WrongPlayer;
                        }
                        return response;
                    }
                    else if (message.message == MessageStrings.serverDataRequest)
                    {
                        Console.WriteLine("playerTurn: " + HostConfigData.playerTurn + " turn: " + HostConfigData.turn + " cycleDuration " + HostConfigData.cycleDuration + " turnDuration " + HostConfigData.turnDuration
                            + " playerTurnName: " + HostConfigData.playerTurnName);
                        var playerTurnNameByteLength = (byte)System.Text.UTF8Encoding.UTF8.GetByteCount(PMCommunication.HostConfigData.playerTurnName);
                        var dataResponse = new byte[18+playerTurnNameByteLength];
                        dataResponse[0] = (byte)Responses.SendingData;
                        Array.Copy(BitConverter.GetBytes(HostConfigData.playerTurn), 0, dataResponse, 1, 4);
                        Console.WriteLine("foo");
                        Array.Copy(BitConverter.GetBytes(HostConfigData.turn), 0, dataResponse, 5, 4);
                        Console.WriteLine("bar");
                        Array.Copy(BitConverter.GetBytes(HostConfigData.cycleDuration), 0, dataResponse, 9, 4);
                        Console.WriteLine("kissa");
                        Array.Copy(BitConverter.GetBytes(HostConfigData.turnDuration), 0, dataResponse, 13, 4);
                        dataResponse[17] = playerTurnNameByteLength;
                        Array.Copy(Encoding.UTF8.GetBytes(HostConfigData.playerTurnName), 0, dataResponse, 18, playerTurnNameByteLength);
                        Console.WriteLine("koira");
                        return dataResponse;
                    }
                }
            }
            response[0] = (byte)Responses.ConnectionRejected;
            return response;
        }
        public void skipPlayerTurn()
        {
            throw new NotImplementedException();
        }
        private void nextTurn()
        {
            PMCommunication.HostConfigData.playerTurn++;
            PMCommunication.HostConfigData.turn++;
            var players = peekPlayers();
            if(PMCommunication.HostConfigData.playerTurn >= players.Count)
            {
                PMCommunication.HostConfigData.playerTurn = 0;
            }
            PMCommunication.HostConfigData.playerTurnName = players[(int)PMCommunication.HostConfigData.playerTurn].name;
            PMCommunication.HostConfigData.saveData();
        }
        private List<Player> peekPlayers()
        {
            List<Player> players = new List<Player>();
            if (!File.Exists(PLAYER_LIST_FILE_NAME))
            {
                
                var file = File.Create(PLAYER_LIST_FILE_NAME);
                file.Close();
            }
            string[] lines = File.ReadAllLines(PLAYER_LIST_FILE_NAME);
            for( var i = 0; i < lines.Length-1; i+=2)
            {
                players.Add(new Player(lines[i], lines[i+1]));
            }
            return players;
        }
        public void addPlayer(string name, string code)
        {
            var playerData = new List<string>
            {
                name,
                code
            };
            File.AppendAllLines("playerList.txt", playerData);
        }

        public void initialize()
        {
            if (!File.Exists(HostConfigData.HOST_CONFIG_FILE_NAME))
            {
                Console.WriteLine("existing config file not found: creating a new one");
                HostConfigData.saveDefaultData();
            }
            else
            {
                Console.WriteLine("found an existing config file");
            }
            HostConfigData.loadData();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var server = new Host();
            server.initialize();
            server.handleConnections();
        }
    }
}
