using ICities;

namespace PM {
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
}