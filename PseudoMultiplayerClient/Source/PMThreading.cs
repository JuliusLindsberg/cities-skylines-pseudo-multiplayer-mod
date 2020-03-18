
using ICities;
using PMCommunication;

namespace PM
{
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
                PseudoMultiplayer.PMSerializingManager.SaveGame(ClientData.SAVE_FILE_NAME + ".crp");
                PMActive = false;
            }

        }
    }
}