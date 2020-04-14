# cities-skylines-pseudo-multiplayer-mod
A cities skylines mod and a dedicated server which provide a sort of turn based multiplayer experience for cities skylines. The idea is to provide functionality equivalent to one player passing a savefile to another with email with the exception of a dedicated server automatically handling transfering the file based on constant timed turns. The length of turns is editable.

As of now I have released neither client or server anywhere else than here. There is currently no way to get prebuilt copies of this.

Based on my limited amount of testing it has most of the basic functionality of a working alpha version such as passing a savefile forward from one client to another via server in a turn based manner. However it's probably still riddled with bugs and is definitely missing critical UI features and documentation. In it's current state figuring it out is probably more trouble than it's worth.

How to build
  client:
    The same way you can build any other Cities: Skylines mod. The game comes with a built-in compiler that automatically makes dll:s out of .cs files that are put into a folder within the mods folder of the game's documentation files that are for windows located in "C:\Users\<your username>\AppData\Local\Colossal Order\Cities_Skylines\Addons\Mods".
  Server:
    You can create a new visual studio project console application and add into it the files communication_types.cs and CitiesSkylinesPseudoMultiServer.cs. For networking stuff the project uses .net framework. communication_types.cs is the only file that is being used both by server and client.
    The dedicated server is intended to be lightweight and cross-compatible with as many things as conveniently possible. Therefore it does not and will not have any sort of GUI. Right now the only way to change it's settings is by editing the text files it creates (and then restarting the server). Some day I might do an actual UI for editing server settings more conveniently who knows.

Port forwarding the server:
  If you have a router then connection attempts to host probably won't get through it. The server uses port 25565 (same port as minecraft). Therefore you should be able to use the same online instructions for port-forwarding your server as you can with a minecraft server.
  
Is it safe?
  I don't know. I'm just a hobbyist who writes random words into .cs files - not a computer security expert. Maybe? As this thing is open source I urge you to evaluate my code for yourself if you feel like it. The only promise I can give is of no malicious intent by me.
  AFAIK the main liability would be if someone inputs their personal information in the client as net traffic is not encrypted in any way. Don't do that.
