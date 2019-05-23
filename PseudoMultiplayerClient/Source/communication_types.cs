using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
//this file is supposed to contain all the code shared between the dedicated server and the clients
namespace PMCommunication
{
    enum Responses : byte
    {
        ConnectionRejected,
        ReceivingSave,
        SendingSave,
        JoinRefused,
        JoinAccepted,
        NameTaken,
        WrongPlayer,
        WrongCode,
        SendingData
    }
    public static class MessageStrings
    {
        public const string joinGame = "join";
        public const string saveToHost = "save";
        public const string saveToClient = "fetchsave";
        public const string serverDataRequest = "serverdata";
    }
    //all data saved in HostData is passed to a client from the server when requested
    public static class HostConfigData
    {
        public const string HOST_CONFIG_FILE_NAME = "hostData.txt";
        public static uint playerTurn { get; set; }
        public static string playerTurnName { get; set; }
        public static uint turn { get; set; }
        //The aim is to give at least roughly the same amount of simulation ticks for each player every turn.
        //A cycleDuration amount of simulation ticks makes up a cycle(decided by host)
        //cycle amount determines turn length, which is expressed in variable turnLength
        public static uint cycleDuration { get; set; }
        public static uint turnDuration { get; set; }
        public static void loadData()
        {
            string[] data = File.ReadAllLines(HOST_CONFIG_FILE_NAME);
            playerTurn = Convert.ToUInt32(data[0]);
            turn = Convert.ToUInt32(data[1]);
            cycleDuration = Convert.ToUInt32(data[2]);
            turnDuration = Convert.ToUInt32(data[3]);
            playerTurnName = data[4];
        }
        public static void saveData()
        {
            string[] data = new string[]
            {
                playerTurn.ToString(),
                turn.ToString(),
                cycleDuration.ToString(),
                turnDuration.ToString(),
                playerTurnName
            };
            File.WriteAllLines(HOST_CONFIG_FILE_NAME, data);
        }
        public static void saveDefaultData()
        {
            string[] data = new string[]
            {
                "0",    // default player turn
                "0",    // default turn number
                "500",  // default cycle duration
                "100",  // default turn Duration
                "First turn"
            };
            File.WriteAllLines(HOST_CONFIG_FILE_NAME, data);
        }
        public static void fetchRemoteData(string name, string code, string ip)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(ip, Message.PORT);
            //ClientData.loadData();

            var message = new PMCommunication.Message(name, code, "serverdata");

            socket.Send(message.messageAsByteArray());
            var buffer = new byte[1024];
            socket.Receive(buffer);
            playerTurn = BitConverter.ToUInt32(buffer, 1);
            turn = BitConverter.ToUInt32(buffer, 5);
            cycleDuration = BitConverter.ToUInt32(buffer, 9);
            turnDuration = BitConverter.ToUInt32(buffer, 13);
            byte playerTurnNameByteLength = buffer[17];
            playerTurnName = Encoding.UTF8.GetString(buffer, 18, (int)playerTurnNameByteLength);
        }
    }

    class Message
    {
        //probably not the right place for this constant
        public const int PORT = 25565;
        //the code is always 5 digits long
        public string code;
        public string name;
        public string message;
        //this was not a good idea after all, as parseMessage does not return void
        /*public Message(byte[] msg)
        {
            parseMessage(msg);
        }*/
        public Message()
        {
            code = ""; name = ""; message = "";
        }
        public Message(string _name, string _code, string _message)
        {
            code = _code; name = _name; message = _message;
        }
        //returns true if message could be parsed properly, false if not
        public bool parseFromString(byte[] msg)
        {
            try
            {
                int codeLength = msg[0];
                int nameLength = msg[1];
                int messageLength = msg[2];
                code = Encoding.UTF8.GetString(msg, 3, codeLength);
                name = Encoding.UTF8.GetString(msg, 3 + codeLength, nameLength);
                message = Encoding.UTF8.GetString(msg, 3 + codeLength + nameLength, messageLength);
                Console.WriteLine("Message from a client parsed successfully!");
                return true;
            }
            catch
            {
                Console.WriteLine("Server Failed to interpret a message from a client!");
                return false;
            }
        }
        public byte[] messageAsByteArray()
        {
            //for now lets just hope this conversion won't cause problems... it shouldn't really as name lengths nor code lengths beyond 100 are not supported anyways...
            byte nameLength = (byte)System.Text.UTF8Encoding.UTF8.GetByteCount(name);
            Console.WriteLine("len name: " + nameLength);
            byte codeLength = (byte)System.Text.UTF8Encoding.UTF8.GetByteCount(code);
            Console.WriteLine("len code: " + codeLength);
            byte messageLength = (byte)System.Text.UTF8Encoding.UTF8.GetByteCount(message);
            Console.WriteLine("message len: " + messageLength);
            var messageArray = new byte[nameLength + codeLength + messageLength + 3];
            messageArray[0] = codeLength;
            messageArray[1] = nameLength;
            messageArray[2] = messageLength;
            var temp = name + code;
            Array.Copy(Encoding.UTF8.GetBytes(code + name + message), 0, messageArray, 3, codeLength + nameLength + messageLength);
            return messageArray;
        }
    }
}
