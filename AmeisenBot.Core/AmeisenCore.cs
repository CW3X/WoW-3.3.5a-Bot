﻿using AmeisenBotLogger;
using AmeisenBotUtilities;
using Magic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace AmeisenBotCore
{
    /// <summary>
    /// Abstract class that contains various static method's to interact with WoW's memory and the
    /// EndScene hook.
    /// </summary>
    public abstract class AmeisenCore
    {
        public static BlackMagic BlackMagic { get; set; }
        public static AmeisenHook AmeisenHook { get; set; }

        /// <summary>
        /// AntiAFK
        /// </summary>
        public static void AntiAFK()
        {
            BlackMagic.WriteInt(Offsets.tickCount, Environment.TickCount);
        }

        /// <summary>
        /// Switch shapeshift forms, use for example "WoWDruid.ShapeshiftForms.Bear"
        /// </summary>
        public static void CastShapeshift(int index)
        {
            AmeisenLogger.Instance.Log(LogLevel.VERBOSE, $"Casting ShapeshiftForm:{index}", "AmeisenCore");
            LuaDoString($"CastShapeshiftForm(\"{index}\");");
        }

        /// <summary>
        /// Cast a spell by its name
        /// </summary>
        public static void CastSpellByName(string spellname, bool onMyself)
        {
            AmeisenLogger.Instance.Log(LogLevel.VERBOSE, $"Casting Spell:{spellname}", "AmeisenCore");
            if (onMyself)
                LuaDoString($"CastSpellByName(\"{spellname}\", true);");
            else
                LuaDoString($"CastSpellByName(\"{spellname}\");");
        }

        /// <summary>
        /// Let the bot jump by pressing the spacebar once for 20-40ms
        ///
        /// This runs Async.
        /// </summary>
        public static void CharacterJumpAsync()
        {
            AmeisenLogger.Instance.Log(LogLevel.VERBOSE, "Jumping", "AmeisenCore");
            new Thread(CharacterJump).Start();
        }

        /// <summary>
        /// Check if the player's world is in a loadingscreen
        /// </summary>
        /// <returns>true if yes, false if no</returns>
        public static bool CheckLoadingScreen()
        {
            return false;
        }

        /// <summary>
        /// Check if the player's world is loaded
        /// </summary>
        /// <returns>true if yes, false if no</returns>
        public static bool CheckWorldLoaded()
        {
            return BlackMagic.ReadInt(Offsets.worldLoaded) == 1;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        /// <summary>
        /// Reads all WoWObject out of WoW's ObjectManager
        /// </summary>
        /// <returns>all WoWObjects in WoW Manager</returns>
        public static List<WowObject> GetAllWoWObjects()
        {
            List<WowObject> objects = new List<WowObject>();

            //try
            //{
                uint currentObjectManager = BlackMagic.ReadUInt(Offsets.currentClientConnection);
                currentObjectManager = BlackMagic.ReadUInt(currentObjectManager + Offsets.currentManagerOffset);

                uint activeObj = BlackMagic.ReadUInt(currentObjectManager + Offsets.firstObjectOffset);
                uint objType = BlackMagic.ReadUInt(activeObj + Offsets.gameobjectTypeOffset);

                ulong myGUID = ReadPlayerGUID();

                // loop through the objects until an object is bigger than 7 or lower than 1 to get
                // all Objects from manager
                while (objType <= 7 && objType > 0)
                {
                    //if (!(refreshOnlyUnits
                    //    && (objType == (int)WoWObjectType.UNIT || objType == (int)WoWObjectType.PLAYER)))
                    //{
                    WowObject wowObject = ReadWoWObjectFromWoW(activeObj, (WowObjectType)objType);
                    objects.Add(wowObject);
                    //}

                    activeObj = BlackMagic.ReadUInt(activeObj + Offsets.nextObjectOffset);
                    objType = BlackMagic.ReadUInt(activeObj + Offsets.gameobjectTypeOffset);
                }
            /*}
            catch (Exception e)
            {
                AmeisenLogger.Instance.Log(LogLevel.ERROR, "Crash at reading Object:" + e, "AmeisenCore");
            }*/

            return objects;
        }

        /// <summary>
        /// Check for Auras/Buffs
        /// </summary>
        /// <returns>true if target has that aura, false if not</returns>
        public static string[] GetAuras(LuaUnit LuaUnit)
        {
            StringBuilder cmdBuffs = new StringBuilder();
            cmdBuffs.Append("local buffs, i = { }, 1;");
            cmdBuffs.Append($"local buff = UnitBuff(\"{LuaUnit.ToString()}\", i);");
            cmdBuffs.Append("while buff do\n");
            cmdBuffs.Append("buffs[#buffs + 1] = buff;");
            cmdBuffs.Append("i = i + 1;");
            cmdBuffs.Append($"buff = UnitBuff(\"{LuaUnit.ToString()}\", i);");
            cmdBuffs.Append("end;");
            cmdBuffs.Append("if #buffs < 1 then\n");
            cmdBuffs.Append("buffs = \"\";");
            cmdBuffs.Append("else\n");
            cmdBuffs.Append("activeUnitBuffs = table.concat(buffs, \", \");");
            cmdBuffs.Append("end;");

            StringBuilder cmdDebuffs = new StringBuilder();
            cmdDebuffs.Append("local buffs, i = { }, 1;");
            cmdDebuffs.Append($"local buff = UnitDebuff(\"{LuaUnit.ToString()}\", i);");
            cmdDebuffs.Append("while buff do\n");
            cmdDebuffs.Append("buffs[#buffs + 1] = buff;");
            cmdDebuffs.Append("i = i + 1;");
            cmdDebuffs.Append($"buff = UnitDebuff(\"{LuaUnit.ToString()}\", i);");
            cmdDebuffs.Append("end;");
            cmdDebuffs.Append("if #buffs < 1 then\n");
            cmdDebuffs.Append("buffs = \"\";");
            cmdDebuffs.Append("else\n");
            cmdDebuffs.Append("activeUnitDebuffs = table.concat(buffs, \", \");");
            cmdDebuffs.Append("end;");

            string[] buffs = GetLocalizedText(cmdBuffs.ToString(), "activeUnitBuffs").Split(',');
            string[] debuffs = GetLocalizedText(cmdDebuffs.ToString(), "activeUnitDebuffs").Split(',');

            List<string> resultLowered = new List<string>();

            foreach (string s in buffs)
                resultLowered.Add(s.Trim().ToLower());
            foreach (string s in debuffs)
                resultLowered.Add(s.Trim().ToLower());

            AmeisenLogger.Instance.Log(LogLevel.VERBOSE, $"UnitAuras: {resultLowered.ToString()}", "AmeisenCore");

            return resultLowered.ToArray();
        }

        /// <summary>
        /// Returns the current combat state
        /// </summary>
        /// <param name="onMyself">check my owm state</param>
        /// <returns>true if unit is in combat, false if not</returns>
        public static bool GetCombatState(LuaUnit LuaUnit)
        {
            AmeisenLogger.Instance.Log(LogLevel.VERBOSE, $"Getting Combat state of: {LuaUnit.ToString()}", "AmeisenCore");

            bool isInCombat = false;
            try { if (int.Parse(GetLocalizedText($"affectingCombat = UnitAffectingCombat(\"{LuaUnit.ToString()}\");", "affectingCombat")) == 1) isInCombat = true; else isInCombat = false; } catch { isInCombat = false; }
            return isInCombat;
        }

        public static Vector3 GetCorpsePosition()
        {
            Vector3 corpsePosition = new Vector3
            (
                BlackMagic.ReadFloat(Offsets.corpseX),
                BlackMagic.ReadFloat(Offsets.corpseY),
                BlackMagic.ReadFloat(Offsets.corpseZ)
            );
            AmeisenLogger.Instance.Log(LogLevel.VERBOSE, $"Getting Corpse Position [{corpsePosition.X},{corpsePosition.Y},{corpsePosition.Z}]", "AmeisenCore");
            return corpsePosition;
        }

        /// <summary>
        /// Get Localized Text for command
        /// </summary>
        /// <param name="command">lua command to run</param>
        public static string GetLocalizedText(string command, string variable)
        {
            if (command.Length > 0 && variable.Length > 0)
            {
                uint argCCCommand = BlackMagic.AllocateMemory(Encoding.UTF8.GetBytes(command).Length + 1);
                BlackMagic.WriteBytes(argCCCommand, Encoding.UTF8.GetBytes(command));

                string[] asmDoString = new string[]
                {
                $"MOV EAX, {(argCCCommand) }",
                "PUSH 0",
                "PUSH EAX",
                "PUSH EAX",
                $"CALL {(Offsets.luaDoString)}",
                "ADD ESP, 0xC",
                "RETN",
                };

                uint argCC = BlackMagic.AllocateMemory(Encoding.UTF8.GetBytes(variable).Length + 1);
                BlackMagic.WriteBytes(argCC, Encoding.UTF8.GetBytes(variable));

                uint playerBase = BlackMagic.ReadUInt(Offsets.playerBase);
                playerBase = BlackMagic.ReadUInt(playerBase + 0x34);
                playerBase = BlackMagic.ReadUInt(playerBase + 0x24);

                string[] asmLocalText = new string[]
                {
                $"CALL {(Offsets.clientObjectManagerGetActivePlayerObject)}",
                "MOV ECX, EAX",
                "PUSH -1",
                $"PUSH {(argCC)}",
                $"CALL {(Offsets.luaGetLocalizedText)}",
                "RETN",
                };

                HookJob hookJobLocaltext = new HookJob(asmLocalText, true);
                ReturnHookJob hookJobDoString = new ReturnHookJob(asmDoString, false, hookJobLocaltext);

                AmeisenHook.AddHookJob(ref hookJobDoString);

                while (!hookJobDoString.IsFinished || !hookJobDoString.IsFinished) { Thread.Sleep(5); }

                string result = Encoding.UTF8.GetString((byte[])hookJobDoString.ReturnValue);

                AmeisenLogger.Instance.Log(LogLevel.VERBOSE, "DoString(" + command + "); => " + variable + " = " + result, "AmeisenCore");

                BlackMagic.FreeMemory(argCCCommand);
                BlackMagic.FreeMemory(argCC);
                return result;
            }
            return "";
        }

        public static int GetMapID()
        {
            return BlackMagic.ReadInt(Offsets.mapID);
        }

        /// <summary>
        /// Run through the WoWObjectManager and find the BaseAdress corresponding to the given GUID
        /// </summary>
        /// <param name="guid">guid to search for</param>
        /// <returns>BaseAdress of the WoWObject</returns>
        public static uint GetMemLocByGUID(ulong guid, List<WowObject> woWObjects)
        {
            AmeisenLogger.Instance.Log(LogLevel.VERBOSE, $"Reading: GUID [{guid}]", "AmeisenCore");

            if (woWObjects != null)
                foreach (WowObject obj in woWObjects)
                    if (obj != null)
                        if (obj.Guid == guid)
                            return obj.BaseAddress;

            return 0;
        }

        /// <summary>
        /// Returns the running WoW's in a WoWExe List containing the logged in playername and
        /// Process object.
        /// </summary>
        /// <returns>A list containing all the runnign WoW processes</returns>
        public static List<WowExe> GetRunningWoWs()
        {
            List<WowExe> wows = new List<WowExe>();
            List<Process> processList = new List<Process>(Process.GetProcessesByName("Wow"));

            foreach (Process p in processList)
            {
                AmeisenLogger.Instance.Log(LogLevel.DEBUG, $"Found WoW Process! PID: {p.Id}", "AmeisenCore");

                WowExe wow = new WowExe();
                BlackMagic blackmagic = new BlackMagic(p.Id);

                wow.characterName = blackmagic.ReadASCIIString(Offsets.playerName, 12);
                wow.process = p;
                wows.Add(wow);
            }

            return wows;
        }

        /// <summary>
        /// Check if the spell is on cooldown
        /// </summary>
        /// <param name="spell">spellname</param>
        /// <returns>true if it is on cooldown, false if not</returns>
        public static SpellInfo GetSpellInfo(string spell)
        {
            SpellInfo info = new SpellInfo();

            string cmd = $"_, _, _, cost, _, _, castTime, _ = GetSpellInfo(\"{spell}\");";

            info.name = spell; //try { info.name = GetLocalizedText("name"); } catch { info.castTime = -1; }
            try { info.castTime = int.Parse(GetLocalizedText(cmd, "castTime")); } catch { info.castTime = -1; }
            //try { info.cost = int.Parse(GetLocalizedText(cmd, "cost")); } catch { info.cost = -1; }

            return info;
        }

        public static CastingInfo GetUnitCastingInfo(LuaUnit player)
        {
            CastingInfo info = new CastingInfo();

            string cmd = $"name, _, _, _, _, endTime _, _, canInterrupt = UnitCastingInfo(\"{player}\");";

            try { info.name = GetLocalizedText(cmd, "name"); } catch { info.name = "none"; }
            try { info.endTime = int.Parse(GetLocalizedText(cmd, "endTime")); } catch { info.endTime = -1; }
            try { info.canInterrupt = bool.Parse(GetLocalizedText(cmd, "canInterrupt")); } catch { info.canInterrupt = false; }

            AmeisenLogger.Instance.Log(LogLevel.DEBUG, $"CastingInfo: [{info.name},{info.endTime},{info.canInterrupt}]", "AmeisenCore");

            return info;
        }

        public static int GetZoneID()
        {
            return BlackMagic.ReadInt(Offsets.zoneID);
        }

        /// <summary> Move the player to the given guid npc, object or whatever and iteract with it.
        /// </summary> <param name="pos">Vector3 containing the X,y & Z coordinates</param> <param
        /// name="guid">guid of the entity</param> <param name="action">CTM Interaction to perform</param>
        public static void InteractWithGUID(Vector3 pos, ulong guid, InteractionType action)
        {
            AmeisenLogger.Instance.Log(LogLevel.DEBUG, $"Interacting: X [{pos.X}] Y [{pos.Y}] Z [{pos.Z}] GUID [{guid}]", "AmeisenCore");
            BlackMagic.WriteUInt64(Offsets.ctmGUID, guid);
            MovePlayerToXYZ(pos, action);
        }

        public static bool IsDead(LuaUnit LuaUnit)
        {
            try
            {
                return int.Parse(
                    GetLocalizedText($"isDead = UnitIsDead(\"{LuaUnit.ToString()}\");"
                    , "isDead")
                ) > 0;
            }
            catch { return false; }
        }

        public static bool IsDeadOrGhost(LuaUnit LuaUnit)
        {
            try
            {
                return int.Parse(
                    GetLocalizedText(
                        $"isDeadOrGhost = UnitIsDeadOrGhost(\"{LuaUnit.ToString()}\");"
                        , "isDeadOrGhost")
                    ) > 0;
            }
            catch { return false; }
        }

        public static bool IsGhost(LuaUnit LuaUnit)
        {
            try
            {
                return int.Parse(
                    GetLocalizedText(
                        $"isGhost = UnitIsDeadOrGhost(\"{LuaUnit.ToString()}\");"
                        , "isGhost")
                    ) > 0;
            }
            catch { return false; }
        }

        /// <summary>
        /// Check if the spell is on cooldown
        /// </summary>
        /// <param name="spell">spellname</param>
        /// <returns>true if it is on cooldown, false if not</returns>
        public static bool IsOnCooldown(string spell)
        {
            try
            {
                return int.Parse(GetLocalizedText(
                    $"start, duration, enabled = GetSpellCooldown(\"{spell}\");"
                    , "duration")
                ) > 0;
            }
            catch { return true; }
        }

        /// <summary>
        /// Returns wether the Unit is Friendly or not
        /// </summary>
        /// <returns>true if unit is friendly, false if not</returns>
        public static bool IsTargetFriendly()
        {
            try
            {
                string text = GetLocalizedText("isFriendly  = UnitAffectingCombat(\"player\", \"target\");", "isFriendly");
                if (int.Parse(text) == 1)
                    return true;
                else return false;
            }
            catch { return false; }
        }

        /// <summary>
        /// Execute the given LUA command inside WoW's MainThread
        /// </summary>
        /// <param name="command">lua command to run</param>
        public static void LuaDoString(string command)
        {
            AmeisenLogger.Instance.Log(LogLevel.VERBOSE, $"Doing string: Command [{command}]", "AmeisenCore");
            uint argCC = BlackMagic.AllocateMemory(Encoding.UTF8.GetBytes(command).Length + 1);
            BlackMagic.WriteBytes(argCC, Encoding.UTF8.GetBytes(command));

            string[] asm = new string[]
            {
                $"MOV EAX, {(argCC)}",
                "PUSH 0",
                "PUSH EAX",
                "PUSH EAX",
                $"CALL {(Offsets.luaDoString)}",
                "ADD ESP, 0xC",
                "RETN",
            };

            HookJob hookJob = new HookJob(asm, false);
            AmeisenHook.AddHookJob(ref hookJob);

            while (!hookJob.IsFinished) { Thread.Sleep(1); }

            AmeisenLogger.Instance.Log(LogLevel.VERBOSE, $"Command returned: Command [{command}]", "AmeisenCore");
            BlackMagic.FreeMemory(argCC);
        }

        /// <summary> Move the Player to the given x, y and z coordinates. </summary> <param
        /// name="pos">Vector3 containing the X,y & Z coordinates</param> <param name="action">CTM
        /// Interaction to perform</param>
        public static void MovePlayerToXYZ(Vector3 pos, InteractionType action)
        {
            AmeisenLogger.Instance.Log(LogLevel.VERBOSE, $"Moving to: X [{pos.X}] Y [{pos.Y}] Z [{pos.Z}]", "AmeisenCore");
            //if (AmeisenManager.Instance.Me().pos.x != pos.x && AmeisenManager.Instance.Me().pos.y != pos.y && AmeisenManager.Instance.Me().pos.z != pos.z)
            //{
            WriteXYZToMemory(pos, action);
            //}
        }

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Get the bot's char's GUID
        /// </summary>
        /// <returns>the GUID</returns>
        public static ulong ReadPlayerGUID()
        {
            return BlackMagic.ReadUInt64(Offsets.localPlayerGUID);
        }

        /// <summary>
        /// Get the bot's char's target's GUID
        /// </summary>
        /// <returns>guid</returns>
        public static ulong ReadTargetGUID()
        {
            return BlackMagic.ReadUInt64(Offsets.localTargetGUID);
        }

        /// <summary>
        /// Read WoWObject from WoW's memory by its GUID/BaseAddress
        /// </summary>
        /// <param name="guid">guid of the object</param>
        /// <param name="baseAddress">baseAddress of the object</param>
        /// <returns>the WoWObject</returns>
        public static WowObject ReadWoWObjectFromWoW(uint baseAddress, WowObjectType woWObjectType, bool isMe = false)
        {
            AmeisenLogger.Instance.Log(LogLevel.VERBOSE, $"Reading: baseAddress [{baseAddress}]", "AmeisenCore");

            if (baseAddress == 0)
                return null;

            switch (woWObjectType)
            {
                case WowObjectType.CONTAINER:
                    return new Container(baseAddress, BlackMagic);

                case WowObjectType.ITEM:
                    return new Item(baseAddress, BlackMagic);

                case WowObjectType.GAMEOBJECT:
                    return new GameObject(baseAddress, BlackMagic);

                case WowObjectType.DYNOBJECT:
                    return new DynObject(baseAddress, BlackMagic);

                case WowObjectType.CORPSE:
                    return new Corpse(baseAddress, BlackMagic);

                case WowObjectType.PLAYER:
                    Player obj = new Player(baseAddress, BlackMagic);

                    if (obj.Guid == ReadPlayerGUID())
                        return new Me(baseAddress, BlackMagic);

                    return obj;

                case WowObjectType.UNIT:
                    return new Unit(baseAddress, BlackMagic);

                default:
                    break;
            }
            return null;
        }

        public static void ReleaseSpirit()
        {
            LuaDoString("RepopMe();");
        }

        public static void RetrieveCorpse()
        {
            int corpseDelay = int.Parse(
                GetLocalizedText($"corpseDelay = GetCorpseRecoveryDelay();"
                , "corpseDelay")
            );

            Thread.Sleep((corpseDelay * 1000) + 100);
            LuaDoString("RetrieveCorpse();");
        }

        /// <summary>
        /// Run the given slash-commando
        /// </summary>
        /// <param name="slashCommand">Example: /target player</param>
        public static void RunSlashCommand(string slashCommand)
        {
            LuaDoString($"DEFAULT_CHAT_FRAME.editBox:SetText(\"{slashCommand}\") ChatEdit_SendText(DEFAULT_CHAT_FRAME.editBox, 0)");
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Target a GUID
        /// </summary>
        /// <param name="guid">guid to target</param>
        public static void TargetGUID(ulong guid)
        {
            AmeisenLogger.Instance.Log(LogLevel.DEBUG, $"TargetGUID: {guid}", "AmeisenCore");
            byte[] guidBytes = BitConverter.GetBytes(guid);

            string[] asm = new string[]
            {
                $"PUSH {BitConverter.ToInt32(guidBytes, 4)}",
                $"PUSH {BitConverter.ToInt32(guidBytes, 0)}",
                $"CALL {(Offsets.clientGameUITarget)}",
                "RETN",
            };

            HookJob hookJob = new HookJob(asm, false);
            AmeisenHook.AddHookJob(ref hookJob);
        }

        private static void CharacterJump()
        {
            SendKey(new IntPtr(0x20));
        }

        private static void PauseMainThread()
        {
            SThread.SuspendThread(
                SThread.OpenThread(
                    SThread.GetMainThread(BlackMagic.ProcessId).Id
                    )
                );
        }

        private static void ResumeMainthread()
        {
            SThread.ResumeThread(
                SThread.OpenThread(
                    SThread.GetMainThread(BlackMagic.ProcessId).Id
                    )
                );
        }

        private static void SendKey(IntPtr vKey)
        {
            const uint KEYDOWN = 0x100;
            const uint KEYUP = 0x101;

            IntPtr windowHandle = BlackMagic.WindowHandle;

            // 0x20 = Spacebar (VK_SPACE)
            SendMessage(windowHandle, KEYDOWN, vKey, new IntPtr(0));
            Thread.Sleep(new Random().Next(20, 40)); // make it look more human-like :^)
            SendMessage(windowHandle, KEYUP, vKey, new IntPtr(0));
        }

        private static void StopMovement()
        {
            //SendKey(new IntPtr(0x26));
            //SendKey(new IntPtr(0x28));
        }

        /// <summary> Write the coordinates and action to the memory. </summary> <param
        /// name="pos">Vector3 containing the X,y & Z coordinates</param> <param name="action">CTM
        /// Interaction to perform</param>
        private static void WriteXYZToMemory(Vector3 pos, InteractionType action, [CallerMemberName]string functionName = "")
        {
            const float distance = 1.5f;

            AmeisenLogger.Instance.Log(LogLevel.DEBUG, $"Writing: X [{pos.X},{pos.Y},{pos.Z}] Action [{action}] Distance [{distance}]", "AmeisenCore", functionName);
            BlackMagic.WriteFloat(Offsets.ctmX, (float)pos.X);
            BlackMagic.WriteFloat(Offsets.ctmY, (float)pos.Y);
            BlackMagic.WriteFloat(Offsets.ctmZ, (float)pos.Z);
            BlackMagic.WriteInt(Offsets.ctmAction, (int)action);
            BlackMagic.WriteFloat(Offsets.ctmDistance, distance);
        }
    }
}