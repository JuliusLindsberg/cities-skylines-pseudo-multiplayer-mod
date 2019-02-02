using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using PMCommunication;

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
        const string PLAYER_LIST_FILE_NAME = "playerList.txt";
        //the string is players name(visible to others) and the int is for login code(not visible, but not encrypted while transmitting it in any way either)
        //ideally, a player should be able to stay oblivious about his own login code, but it should also be possible to check or change in-game via cities skylines mod options
        public Host()
        {
        }
        public void handleConnections()
        {

            Message message1 = new Message("perkele", "1234", "asdf");

            var asByteArray = message1.messageAsByteArray();
            Console.WriteLine("DANGERZONE");
            for(int i = 0; i < asByteArray.Length; i++)
            {
                Console.Write(asByteArray[i]);
            }
            Console.WriteLine("\nDANGERZONE END");
            Console.WriteLine("asdasdasd");
            Console.WriteLine(asByteArray);
            var message2 = new Message();
            Console.WriteLine("kekekekke");
            message2.parseFromString(asByteArray);

            Console.WriteLine(message2.name + " - " + message2.code + " - " + message2.message);

            Console.WriteLine("Opening the connections!");
            //shamelessly copy-pasted from https://stackoverflow.com/questions/8655980/how-to-receive-a-file-over-tcp-which-was-sent-using-socket-filesend-method
            //it seemed to work all right so if it ain't broken don't change it :s
            TcpListener listener = new TcpListener(Message.PORT);
            listener.Start();
            while (true)
            {
                //listener.AcceptTCPClient()
                using (var client = listener.AcceptSocket())
                //using (var stream = client.GetStream())
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
                        Console.WriteLine("Invalid operation was requested by the server. Connection with client was closed.");
                    }
                    else if (reply[0] == (byte)Responses.ReceivingSave)
                    {
                        using (var output = File.Create(PMCommunication.HostConfigData.saveFileName))
                        {
                            // read the file in chunks of 1KB
                            var buffer = new byte[1024];
                            int bytesRead;
                            while ((bytesRead = client.Receive(buffer)) > 0)
                            {
                                output.Write(buffer, 0, bytesRead);
                            }
                        }
                        Console.WriteLine("Connection ended successfully!");
                    }
                    else if (reply[0] == (byte)Responses.SendingSave)
                    {
                        client.SendFile(PMCommunication.HostConfigData.saveFileName);
                    }
                    else
                    {
                        Console.WriteLine("Sending a reply.");
                        //stream.Write(reply, 0, reply.Length);
                        client.Send(reply, reply.Length, SocketFlags.None);
                        Console.WriteLine("The client received the reply successfully.");
                        client.Close();
                    }
                }
            }
        }
        byte[] reactToMessage(Message message)
        {
            var response = new byte[1];
            Console.WriteLine("Message! name: " + message.name + ", code: " + message.code + ", message: " + message.message + ".");
            List<Player> players = peekPlayers();
            if (message.message == "join")
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
                    if (message.message == "save")
                    {
                        Console.WriteLine("message: save, Receiving save");
                        response[0] = (byte)Responses.ReceivingSave;
                        return response;
                    }
                    else if (message.message == "fetchsave")
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
                    else if (message.message == "serverdata")
                    {

                        var dataResponse = new byte[18+System.Text.UTF8Encoding.Unicode.GetByteCount(HostConfigData.saveFileName)];
                        dataResponse[0] = response[0];
                        Array.Copy(BitConverter.GetBytes(HostConfigData.playerTurn), 0, dataResponse, 1, 4);
                        Array.Copy(BitConverter.GetBytes(HostConfigData.turn), 0, dataResponse, 5, 4);
                        Array.Copy(BitConverter.GetBytes(HostConfigData.cycleDuration), 0, dataResponse, 9, 4);
                        Array.Copy(BitConverter.GetBytes(HostConfigData.turnDuration), 0, dataResponse, 13, 4);
                        dataResponse[17] = (byte)System.Text.ASCIIEncoding.Unicode.GetByteCount(HostConfigData.saveFileName);
                        Array.Copy(System.Text.Encoding.UTF8.GetBytes(HostConfigData.saveFileName), 0, dataResponse, 18, System.Text.Encoding.Unicode.GetByteCount(HostConfigData.saveFileName));
                        return dataResponse;
                    }
                }
            }
            response[0] = (byte)Responses.ConnectionRejected;
            return response;
        }
        public void skipPlayerTurn()
        {

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
    }

    class Program
    {
        static void Main(string[] args)
        {
            var server = new Host();
            server.handleConnections();
        }
    }
}
