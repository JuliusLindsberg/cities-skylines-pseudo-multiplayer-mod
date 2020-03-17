using ICities;
using PMCommunication;

namespace PM {
    public class PMLoader : LoadingExtensionBase
    {
        public override void OnCreated(ILoading loading)
        {
            //asks the server about who's turn is it right now
            ClientData.loadData();
            TurnData.loadTurnData();
            if (ClientData.hostIP != "0.0.0.0")
            {
                try
                {
                    HostConfigData.fetchRemoteData(ClientData.name, ClientData.code, ClientData.hostIP);
                    if(HostConfigData.playerTurnName == ClientData.name && !TurnData.saveFetched)
                    {
                        PseudoMultiplayer.receiveSaveFromServer();
                    }
                }
                catch
                {
                    ClientData.connectedToHost = false;
                    ClientData.saveData();
                }
                if (HostConfigData.playerTurnName == ClientData.name)
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
            if (ClientData.hostIP == "0.0.0.0")
            {
                PMThreading.PMActive = false;
                return;
            }
            HostConfigData.fetchRemoteData(ClientData.name, ClientData.code, ClientData.hostIP);
            if (HostConfigData.playerTurnName == ClientData.name || HostConfigData.turn == 0)
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
}