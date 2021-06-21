using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Eleon.Modding;
//using ProtoBuf;
using YamlDotNet.Serialization;


namespace CreditSaver
{
    public class MyEmpyrionMod : ModInterface
    {
        public static string ModVersion = "CreditSaver v0.0.2";
        public static string ModPath = "..\\Content\\Mods\\CreditSaver\\";
        internal static bool debug = true;
        internal static Dictionary<int, Storage.StorableData> SeqNrStorage = new Dictionary<int, Storage.StorableData> { };
        public int thisSeqNr = 2000;
        internal static SetupYaml.Root SetupYamlData = new SetupYaml.Root { };
        public ItemStack[] blankItemStack = new ItemStack[] { };

        //########################################################################################################################################################
        //internal List<int> OnlinePlayers = new List<int> { };
        int TickCounter = 0;
        int TickStart = 300;
        internal static Dictionary<int, PlayerYaml.Root> DictPlayerYaml = new Dictionary<int, PlayerYaml.Root> { };
        string BuildNumber = "zero";
        internal static Dictionary<int, PlayerInfo> DictOnlinePlayers = new Dictionary<int, PlayerInfo> { };

        //########################################################################################################################################################
        //################################################ This is where the actual Empyrion Modding API stuff Begins ############################################
        //########################################################################################################################################################
        public void Game_Start(ModGameAPI gameAPI)
        {
            Storage.GameAPI = gameAPI;
            if (debug) { File.WriteAllText(ModPath + "ERROR.txt", ""); }
            if (debug) { File.WriteAllText(ModPath + "debug.txt", ""); }
            SetupYaml.Setup();
            CommonFunctions.Log("--------------------" + " Server Start " + CommonFunctions.TimeStamp() + "----------------------------");

            try
            {
                string[] BuildNumberTxt = File.ReadAllLines("..\\BuildNumber.txt");
                BuildNumber = BuildNumberTxt[0].Trim(' ');
            }
            catch
            {
                CommonFunctions.Debug("Failed getting BuildNumber");
            }
        }

        public void Game_Event(CmdId cmdId, ushort seqNr, object data)
        {
            try
            {
                switch (cmdId)
                {
                    case CmdId.Event_ChatMessage:
                        //Triggered when player says something in-game
                        ChatInfo Received_ChatInfo = (ChatInfo)data;
                        
                        string msg = Received_ChatInfo.msg.ToLower();
                        if (msg == SetupYamlData.ReinitializeCommand) //Reinitialize
                        {
                            SetupYaml.Setup();
                            API.Chat("Player", Received_ChatInfo.playerId, "Reinit Complete");
                        }
                        else if (msg == "/mods")
                        {
                            API.Chat("Player", Received_ChatInfo.playerId, ModVersion);
                        }

                        /*
                        else if (msg == SetupYamlData.USGMarketplaceCommand.ToLower()) //List playfields with faction Marketplaces and Mark marketplaces on current map.
                        {
                            try
                            {
                                Storage.StorableData function = new Storage.StorableData
                                {
                                    function = "MarketPlace",
                                    Match = Convert.ToString(Received_ChatInfo.playerId),
                                    Requested = "PlayerInfo",
                                    ChatInfo = Received_ChatInfo
                                };
                                thisSeqNr = API.PlayerInfo(Received_ChatInfo.playerId);
                                SeqNrStorage[thisSeqNr] = function;
                            }
                            catch
                            {
                                //CommonFunctions.Debug("Marketplace Fail Chat");
                            }
                        }
                        else if (msg == SetupYamlData.USGMarketplaceCommand.ToLower() + " pricecheck")
                        {
                            API.Chat("Player", Received_ChatInfo.playerId, "Marketplace Pricecheck:");
                            foreach (string line in SetupYaml.Bark)
                            {
                                API.Chat("Player", Received_ChatInfo.playerId, line);
                            }
                        }*/
                        break;


                    case CmdId.Event_Player_Connected:
                        //Triggered when a player logs on
                        Id Received_PlayerConnected = (Id)data;
                        try
                        {
                            Storage.StorableData function = new Storage.StorableData
                            {
                                function = "LogOn",
                                Match = Convert.ToString(Received_PlayerConnected.id),
                                Requested = "PlayerInfo"
                            };
                            thisSeqNr = API.PlayerInfo(Received_PlayerConnected.id);
                            SeqNrStorage[thisSeqNr] = function;
                        }
                        catch
                        {
                            CommonFunctions.Debug("LogOn Fail: Player Connect");
                        }
                        break;


                    case CmdId.Event_Player_Disconnected:
                        //Triggered when a player logs off
                        Id Received_PlayerDisconnected = (Id)data;
                        try
                        {
                            if (DictOnlinePlayers.ContainsKey(Received_PlayerDisconnected.id))
                            {
                                PlayerYaml.Root PlayerHistoryFile = new PlayerYaml.Root { };
                                if (File.Exists(ModPath + "Players\\" + DictOnlinePlayers[Received_PlayerDisconnected.id].steamId + ".yaml"))
                                {
                                    PlayerHistoryFile = PlayerYaml.ReadYaml(DictOnlinePlayers[Received_PlayerDisconnected.id].steamId);
                                }
                                else
                                {
                                    PlayerHistoryFile.PlayerName = DictOnlinePlayers[Received_PlayerDisconnected.id].playerName;
                                    PlayerHistoryFile.History = new List<PlayerYaml.History> { };
                                }
                                string NewTimestamp = CommonFunctions.TimeStamp();
                                PlayerYaml.History NewEntry = new PlayerYaml.History
                                {
                                    PlayerID = Received_PlayerDisconnected.id,
                                    credits = DictOnlinePlayers[Received_PlayerDisconnected.id].credits,
                                    BuildNumber = BuildNumber,
                                    LogoffCoordinates = DictOnlinePlayers[Received_PlayerDisconnected.id].pos.x + "," + DictOnlinePlayers[Received_PlayerDisconnected.id].pos.y + "," + DictOnlinePlayers[Received_PlayerDisconnected.id].pos.z,
                                    LogoffPlayfield = DictOnlinePlayers[Received_PlayerDisconnected.id].playfield,
                                    Timestamp = NewTimestamp
                                };
                                List<PlayerYaml.History> PlayerHistory = PlayerHistoryFile.History;
                                PlayerHistory.Add(NewEntry);
                                PlayerHistoryFile.History = PlayerHistory;
                                PlayerYaml.WriteYaml(DictOnlinePlayers[Received_PlayerDisconnected.id].steamId, PlayerHistoryFile);
                                CommonFunctions.Log("Updated Player History: " + DictOnlinePlayers[Received_PlayerDisconnected.id].steamId);
                            }
                        }
                        catch
                        {
                            CommonFunctions.Debug("LogOff Fail: Player Disconnect");
                        }
                        break;


                    case CmdId.Event_Player_ChangedPlayfield:
                        //Triggered when a player changes playfield
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_ChangePlayfield, (ushort)CurrentSeqNr, new IdPlayfieldPositionRotation( [PlayerID], [Playfield Name], [PVector3 position], [PVector3 Rotation] ));
                        IdPlayfield Received_PlayerChangedPlayfield = (IdPlayfield)data;
                        break;


                    case CmdId.Event_Playfield_Loaded:
                        //Triggered when a player goes to a playfield that isnt currently loaded in memory
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Load_Playfield, (ushort)CurrentSeqNr, new PlayfieldLoad( [float nSecs], [string nPlayfield], [int nProcessId] ));
                        PlayfieldLoad Received_PlayfieldLoaded = (PlayfieldLoad)data;
                        break;


                    case CmdId.Event_Playfield_Unloaded:
                        //Triggered when there are no players left in a playfield
                        PlayfieldLoad Received_PlayfieldUnLoaded = (PlayfieldLoad)data;
                        break;


                    case CmdId.Event_Faction_Changed:
                        //Triggered when an Entity (player too?) changes faction
                        FactionChangeInfo Received_FactionChange = (FactionChangeInfo)data;
                        break;


                    case CmdId.Event_Statistics:
                        //Triggered on various game events like: Player Death, Entity Power on/off, Remove/Add Core
                        StatisticsParam Received_EventStatistics = (StatisticsParam)data;
                        break;


                    case CmdId.Event_Player_DisconnectedWaiting:
                        //Triggered When a player is having trouble logging into the server
                        Id Received_PlayerDisconnectedWaiting = (Id)data;
                        CommonFunctions.Debug("Event_Player_DisconnectedWaiting: " + Received_PlayerDisconnectedWaiting.id);
                        break;


                    case CmdId.Event_TraderNPCItemSold:
                        //Triggered when a player buys an item from a trader
                        TraderNPCItemSoldInfo Received_TraderNPCItemSold = (TraderNPCItemSoldInfo)data;
                        break;


                    case CmdId.Event_Player_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_List, (ushort)CurrentSeqNr, null));
                        IdList Received_PlayerList = (IdList)data;
                        break;


                    case CmdId.Event_Player_Info:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_Info, (ushort)CurrentSeqNr, new Id( [playerID] ));
                        PlayerInfo Received_PlayerInfo = (PlayerInfo)data;
                        //CommonFunctions.Debug("PlayerInfo Received: " + Received_PlayerInfo.steamId);
                        if (SeqNrStorage.Keys.Contains(seqNr))
                        {
                            Storage.StorableData RetrievedData = SeqNrStorage[seqNr];
                            if (RetrievedData.Requested == "PlayerInfo" && RetrievedData.function == "LogOn" && Convert.ToString(Received_PlayerInfo.entityId) == RetrievedData.Match)
                            {
                                SeqNrStorage.Remove(seqNr);
                                if ( File.Exists(MyEmpyrionMod.ModPath + "Players\\" + Received_PlayerInfo.steamId + ".yaml"))
                                {
                                    PlayerYaml.Root OldData = PlayerYaml.ReadYaml(Received_PlayerInfo.steamId);
                                    if ( OldData.History.Last().BuildNumber != BuildNumber && Received_PlayerInfo.entityId != OldData.History.Last().PlayerID)
                                    {
                                        API.CreditsSet(Received_PlayerInfo.entityId, OldData.History.Last().credits);
                                    }
                                }
                                try
                                {
                                    if (DictOnlinePlayers.ContainsKey(Received_PlayerInfo.entityId))
                                    {
                                        DictOnlinePlayers[Received_PlayerInfo.entityId] = Received_PlayerInfo;
                                    }
                                    else
                                    {
                                        DictOnlinePlayers.Add(Received_PlayerInfo.entityId, Received_PlayerInfo);
                                    }
                                }
                                catch
                                {
                                    CommonFunctions.Debug("Fail: store player info");
                                }
                            }
                            if (RetrievedData.Requested == "PlayerInfo" && RetrievedData.function == "Update" && Convert.ToString(Received_PlayerInfo.entityId) == RetrievedData.Match)
                            {
                                SeqNrStorage.Remove(seqNr);
                                try
                                {
                                    if (DictOnlinePlayers.ContainsKey(Received_PlayerInfo.entityId))
                                    {
                                        DictOnlinePlayers[Received_PlayerInfo.entityId] = Received_PlayerInfo;
                                    }
                                    else
                                    {
                                        DictOnlinePlayers.Add(Received_PlayerInfo.entityId, Received_PlayerInfo);
                                    }
                                }
                                catch
                                {
                                    CommonFunctions.Debug("Fail: store player info");
                                }
                            }
                        }
                        break;


                    case CmdId.Event_Player_Inventory:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_GetInventory, (ushort)CurrentSeqNr, new Id( [playerID] ));
                        Inventory Received_PlayerInventory = (Inventory)data;
                        break;


                    case CmdId.Event_Player_ItemExchange:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_ItemExchange, (ushort)CurrentSeqNr, new ItemExchangeInfo( [id], [title], [description], [buttontext], [ItemStack[]] ));
                        ItemExchangeInfo Received_ItemExchangeInfo = (ItemExchangeInfo)data;
                        break;


                    case CmdId.Event_DialogButtonIndex:
                        //All of This is a Guess
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_ShowDialog_SinglePlayer, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Save/Pos = 0, Close/Cancel/Neg = 1
                        IdAndIntValue Received_DialogButtonIndex = (IdAndIntValue)data;
                        break;


                    case CmdId.Event_Player_Credits:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_Credits, (ushort)CurrentSeqNr, new Id( [PlayerID] ));
                        IdCredits Received_PlayerCredits = (IdCredits)data;
                        break;


                    case CmdId.Event_Player_GetAndRemoveInventory:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_GetAndRemoveInventory, (ushort)CurrentSeqNr, new Id( [playerID] ));
                        Inventory Received_PlayerGetRemoveInventory = (Inventory)data;
                        break;


                    case CmdId.Event_Playfield_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Playfield_List, (ushort)CurrentSeqNr, null));
                        PlayfieldList Received_PlayfieldList = (PlayfieldList)data;
                        break;


                    case CmdId.Event_Playfield_Stats:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Playfield_Stats, (ushort)CurrentSeqNr, new PString( [Playfield Name] ));
                        PlayfieldStats Received_PlayfieldStats = (PlayfieldStats)data;
                        break;


                    case CmdId.Event_Playfield_Entity_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Playfield_Entity_List, (ushort)CurrentSeqNr, new PString( [Playfield Name] ));
                        PlayfieldEntityList Received_PlayfieldEntityList = (PlayfieldEntityList)data;
                        break;


                    case CmdId.Event_Dedi_Stats:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Dedi_Stats, (ushort)CurrentSeqNr, null));
                        DediStats Received_DediStats = (DediStats)data;
                        //Received_DediStats.players
                        break;


                    case CmdId.Event_GlobalStructure_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_GlobalStructure_List, (ushort)CurrentSeqNr, null));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_GlobalStructure_Update, (ushort)CurrentSeqNr, new PString( [Playfield Name] ));
                        GlobalStructureList Received_GlobalStructureList = (GlobalStructureList)data;
                        break;


                    case CmdId.Event_Entity_PosAndRot:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_PosAndRot, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        IdPositionRotation Received_EntityPosRot = (IdPositionRotation)data;
                        break;


                    case CmdId.Event_Get_Factions:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Get_Factions, (ushort)CurrentSeqNr, new Id( [int] )); //Requests all factions from a certain Id onwards. If you want all factions use Id 1.
                        FactionInfoList Received_FactionInfoList = (FactionInfoList)data; // List<FactionInfo>
                        break;


                    case CmdId.Event_NewEntityId:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_NewEntityId, (ushort)CurrentSeqNr, null));
                        Id Request_NewEntityId = (Id)data;
                        break;


                    case CmdId.Event_Structure_BlockStatistics:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Structure_BlockStatistics, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        IdStructureBlockInfo Received_StructureBlockStatistics = (IdStructureBlockInfo)data;
                        break;


                    case CmdId.Event_AlliancesAll:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_AlliancesAll, (ushort)CurrentSeqNr, null));
                        AlliancesTable Received_AlliancesAll = (AlliancesTable)data;
                        break;


                    case CmdId.Event_AlliancesFaction:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_AlliancesFaction, (ushort)CurrentSeqNr, new AlliancesFaction( [int nFaction1Id], [int nFaction2Id], [bool nIsAllied] ));
                        AlliancesFaction Received_AlliancesFaction = (AlliancesFaction)data;
                        break;


                    case CmdId.Event_BannedPlayers:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_GetBannedPlayers, (ushort)CurrentSeqNr, null ));
                        BannedPlayerData Received_BannedPlayers = (BannedPlayerData)data;
                        break;


                    case CmdId.Event_GameEvent:
                        //Triggered by PDA Events
                        GameEventData Received_GameEvent = (GameEventData)data;
                        //Received_GameEvent.
                        break;


                    case CmdId.Event_Ok:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_SetInventory, (ushort)CurrentSeqNr, new Inventory(){ [changes to be made] });
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_AddItem, (ushort)CurrentSeqNr, new IdItemStack(){ [changes to be made] });
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_SetCredits, (ushort)CurrentSeqNr, new IdCredits( [PlayerID], [Double] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_AddCredits, (ushort)CurrentSeqNr, new IdCredits( [PlayerID], [+/- Double] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Blueprint_Finish, (ushort)CurrentSeqNr, new Id( [PlayerID] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Blueprint_Resources, (ushort)CurrentSeqNr, new BlueprintResources( [PlayerID], [List<ItemStack>], [bool ReplaceExisting?] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Teleport, (ushort)CurrentSeqNr, new IdPositionRotation( [EntityId OR PlayerID], [Pvector3 Position], [Pvector3 Rotation] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_ChangePlayfield , (ushort)CurrentSeqNr, new IdPlayfieldPositionRotation( [EntityId OR PlayerID], [Playfield],  [Pvector3 Position], [Pvector3 Rotation] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Destroy, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Destroy2, (ushort)CurrentSeqNr, new IdPlayfield( [EntityID], [Playfield] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_SetName, (ushort)CurrentSeqNr, new Id( [EntityID] )); Wait, what? This one doesn't make sense. This is what the Wiki says though.
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Spawn, (ushort)CurrentSeqNr, new EntitySpawnInfo()); Doesn't make sense to me.
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Structure_Touch, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_InGameMessage_SinglePlayer, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_InGameMessage_Faction, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_InGameMessage_AllPlayers, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_ConsoleCommand, (ushort)CurrentSeqNr, new PString( [Telnet Command] ));

                        //uh? Not Listed in Wiki... Received_ = ()data;
                        break;


                    case CmdId.Event_Error:
                        //Triggered when there is an error coming from the API
                        ErrorInfo Received_ErrorInfo = (ErrorInfo)data;
                        if (SeqNrStorage.Keys.Contains(seqNr))
                        {
                            Storage.StorableData RetrievedData = SeqNrStorage[seqNr];
                            SeqNrStorage.Remove(seqNr);
                            if (RetrievedData.Requested == "PlayerInfo" && RetrievedData.function == "Update")
                            {
                                //DictOnlinePlayers.Remove(Int32.Parse(RetrievedData.Match));
                            }
                            else { 
                                CommonFunctions.LogFile("Debug.txt", "API Error:");
                                CommonFunctions.LogFile("Debug.txt", "ErrorType: " + Received_ErrorInfo.errorType);
                                CommonFunctions.LogFile("Debug.txt", "Match: " + RetrievedData.Match);
                            }
                        }
                        break;


                    case CmdId.Event_PdaStateChange:
                        //Triggered by PDA: chapter activated/deactivated/completed
                        PdaStateInfo Received_PdaStateChange = (PdaStateInfo)data;
                        break;


                    case CmdId.Event_ConsoleCommand:
                        //Triggered when a player uses a Console Command in-game
                        ConsoleCommandInfo Received_ConsoleCommandInfo = (ConsoleCommandInfo)data;
                        break;


                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                CommonFunctions.LogFile("ERROR.txt", "Message: " + ex.Message);
                CommonFunctions.LogFile("ERROR.txt", "Data: " + ex.Data);
                CommonFunctions.LogFile("ERROR.txt", "HelpLink: " + ex.HelpLink);
                CommonFunctions.LogFile("ERROR.txt", "InnerException: " + ex.InnerException);
                CommonFunctions.LogFile("ERROR.txt", "Source: " + ex.Source);
                CommonFunctions.LogFile("ERROR.txt", "StackTrace: " + ex.StackTrace);
                CommonFunctions.LogFile("ERROR.txt", "TargetSite: " + ex.TargetSite);
                CommonFunctions.LogFile("ERROR.txt", "");
            }
        }
        public void Game_Update()
        {
            //Triggered whenever Empyrion experiences "Downtime", roughly 75-100 times per second
            TickCounter++;
            //CommonFunctions.Debug("TickCounter: " + TickCounter + "   TickStart: "+ TickStart + "   SetupYamlDelay: " + SetupYamlData.DelayTicks);
            if ( TickCounter > (TickStart + SetupYamlData.DelayTicks))
            {
                TickStart = TickCounter;
                foreach (int player in DictOnlinePlayers.Keys)
                {
                    try
                    {
                        Storage.StorableData function = new Storage.StorableData
                        {
                            function = "Update",
                            Match = Convert.ToString(player),
                            Requested = "PlayerInfo"
                        };
                        thisSeqNr = API.PlayerInfo(player);
                        SeqNrStorage[thisSeqNr] = function;
                    }
                    catch
                    {
                        CommonFunctions.Debug("Update Fail: PlayerInfo request");
                    }
                }
            }
        }
        public void Game_Exit()
        {
            //Triggered when the server is Shutting down. Does NOT pause the shutdown.
        }
    }
}