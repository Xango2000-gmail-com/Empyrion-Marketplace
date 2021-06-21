using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eleon.Modding;

namespace CreditSaver
{
    class API
    {
        public static void Alert(int Target, string Message, string Alert, float Time)
        {
            byte prio = 2;
            if (Alert.ToLower() == "Red".ToLower())
            {
                prio = 0;
            }
            else if (Alert.ToLower() == "Yellow".ToLower())
            {
                prio = 1;
            }
            else
            {
                prio = 2;
            }

            if (Target == 0)
            {
                Storage.GameAPI.Game_Request(CmdId.Request_InGameMessage_AllPlayers, (ushort)Storage.CurrentSeqNr, new IdMsgPrio(0, Message, prio, Time));
            }
            else if (Target < 999)
            {
                Storage.GameAPI.Game_Request(CmdId.Request_InGameMessage_Faction, (ushort)Storage.CurrentSeqNr, new IdMsgPrio(Target, Message, prio, Time));
            }
            else if (Target > 999)
            {
                Storage.GameAPI.Game_Request(CmdId.Request_InGameMessage_SinglePlayer, (ushort)Storage.CurrentSeqNr, new IdMsgPrio(Target, Message, prio, Time));
            }
        }

        public static void Chat(string Type, int Target, string Message)
        {
            if (Type == "Global")
            {
                API.ConsoleCommand("say '" + Message + "'");
            }
            else if (Type == "Faction")
            {
                API.ConsoleCommand("say f:" + Target + " '" + Message + "'");
            }
            else if (Type == "Player")
            {
                API.ConsoleCommand("say p:" + Target + " '" + Message + "'");
                API.Alert(Target, "Private Message Received", "Blue", 3);
            }
        }

        public static int PlayerInfo(int playerID)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            Storage.GameAPI.Game_Request(CmdId.Request_Player_Info, (ushort)Storage.CurrentSeqNr, new Id(playerID));
            return Storage.CurrentSeqNr;
        }

        public static void PlayerInfoChange(PlayerInfoSet PlayerInfoSettings)
        {
            Storage.GameAPI.Game_Request(CmdId.Request_Player_SetPlayerInfo , (ushort)Storage.CurrentSeqNr, PlayerInfoSettings);
        }

        public static int TextWindowOpen(int TargetPlayer, string Message, string ConfirmText, string CancelText)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            Storage.GameAPI.Game_Request(CmdId.Request_ShowDialog_SinglePlayer, (ushort)Storage.CurrentSeqNr, new DialogBoxData()
            {
                Id = Convert.ToInt32(TargetPlayer),
                MsgText = Message,
                NegButtonText = CancelText,
                PosButtonText = ConfirmText
            });
            return Storage.CurrentSeqNr;
        }

        public static int Gents(string playfield)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            Storage.GameAPI.Game_Request(CmdId.Request_GlobalStructure_Update, (ushort)Storage.CurrentSeqNr, new PString(playfield));
            return Storage.CurrentSeqNr;
        }

        public static void ConsoleCommand(string Sendable)
        {
            Storage.GameAPI.Game_Request(CmdId.Request_ConsoleCommand, (ushort)Storage.CurrentSeqNr, new PString(Sendable));
        }

        public static int OpenItemExchange(int PlayerID, string Title, string Message, string ButtonText, ItemStack[] Inventory)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            Storage.GameAPI.Game_Request(CmdId.Request_Player_ItemExchange, (ushort)Storage.CurrentSeqNr, new ItemExchangeInfo(PlayerID, Title, Message, ButtonText, Inventory));
            return Storage.CurrentSeqNr;
        }

        public static void Credits(int PlayerID, Double CreditChange)
        {
            Storage.GameAPI.Game_Request(CmdId.Request_Player_AddCredits, (ushort)Storage.CurrentSeqNr, new IdCredits(PlayerID, CreditChange));
        }

        public static void CreditsSet(int PlayerID, Double Credits)
        {
            Storage.GameAPI.Game_Request(CmdId.Request_Player_SetCredits, (ushort)Storage.CurrentSeqNr, new IdCredits(PlayerID, Credits));
        }

        public static int CreditQuerry(int PlayerID)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            Storage.GameAPI.Game_Request(CmdId.Request_Player_Credits, (ushort)Storage.CurrentSeqNr, new Id(PlayerID));
            return Storage.CurrentSeqNr;
        }

        public static int EntityData(int EntityID)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            Storage.GameAPI.Game_Request(CmdId.Request_Structure_BlockStatistics, (ushort)Storage.CurrentSeqNr, new Id(EntityID));
            return Storage.CurrentSeqNr;
        }

        public static void Delete(int EntityID)
        {
            Storage.GameAPI.Game_Request(CmdId.Request_Entity_Destroy, (ushort)Storage.CurrentSeqNr, new Id(EntityID));
        }

        public static int SetLevel(int ClientID, int Level)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            string Sendable = "remoteex cl=" + ClientID + " \'level = " + Level + "\'";
            Storage.GameAPI.Game_Request(CmdId.Request_ConsoleCommand, (ushort)Storage.CurrentSeqNr, new PString(Sendable));
            if (Level == 25)
            {
                Sendable = "remoteex cl=" + ClientID + " \'level x+ 1\'";
                Storage.GameAPI.Game_Request(CmdId.Request_ConsoleCommand, (ushort)Storage.CurrentSeqNr, new PString(Sendable));

            }
            return Storage.CurrentSeqNr;
        }

        public static int PlayfieldEntityList(string Playfield)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            Storage.GameAPI.Game_Request(CmdId.Request_Playfield_Entity_List, (ushort)Storage.CurrentSeqNr, Playfield);
            return Storage.CurrentSeqNr;
        }

        public static int FactionInfo()
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            Storage.GameAPI.Game_Request(CmdId.Event_Get_Factions, (ushort)Storage.CurrentSeqNr, new Id(1));
            return Storage.CurrentSeqNr;
        }

        public static void GlobalStructureList(String Playfield)
        {
            Storage.GameAPI.Game_Request(CmdId.Event_GlobalStructure_List, (ushort)Storage.CurrentSeqNr, new PString(Playfield));
        }
    }
}
