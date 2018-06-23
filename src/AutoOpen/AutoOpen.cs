using System;
using System.Collections.Generic;
using System.Windows.Forms;
using AutoOpen.Utils;
using PoeHUD.Framework.Helpers;
using PoeHUD.Models;
using PoeHUD.Plugins;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.RemoteMemoryObjects;
using SharpDX;
using SharpDX.Direct3D9;
using System.Linq;
using System.Threading;
using System.IO;

namespace AutoOpen
{
    internal class AutoOpen : BaseSettingsPlugin<Settings>
    {
        private IngameState ingameState;
        private Dictionary<long, int> clickedEntities = new Dictionary<long, int>();
        private List<EntityWrapper> entities = new List<EntityWrapper>();
        private Vector2 windowOffset = new Vector2();
        private List<String> doorBlacklist;
        private List<String> switchBlacklist;
        private List<String> chestWhitelist;


        public AutoOpen()
        {
            PluginName = "AutoOpen";
        }

        public override void Initialise()
        {
            ingameState = GameController.Game.IngameState;
            windowOffset = GameController.Window.GetWindowRectangle().TopLeft;
            loadDoorBlacklist();
            loadSwitchBlacklist();
            loadChestWhitelist();
            base.Initialise();

        }

        public override void Render()
        {
            if (!Settings.Enable) return;
            open();
        }

        public override void EntityAdded(EntityWrapper entity)
        {
            base.EntityAdded(entity);
            if (entity.HasComponent<Render>()
                && (entity.HasComponent<TriggerableBlockage>()
                    || entity.HasComponent<Transitionable>()
                    || entity.HasComponent<Chest>()
                    || entity.HasComponent<Shrine>()
                    || entity.Path.ToLower().Contains("darkshrine"))
                && entity.Address != GameController.Player.Address)
            {
                entities.Add(entity);
            }
        }

        public override void EntityRemoved(EntityWrapper entityWrapper)
        {
            base.EntityRemoved(entityWrapper);
            entities.Remove(entityWrapper);
        }

        public override void OnClose()
        {
            base.OnClose();
        }

        private void open()
        {
            var camera = ingameState.Camera;
            var playerPos = GameController.Player.Pos;
            var prevMousePosition = Mouse.GetCursorPosition();


            foreach (EntityWrapper entity in entities)
            {
                var entityPos = entity.Pos;
                var entityScreenPos = camera.WorldToScreen(entityPos.Translate(0, 0, 0), entity);
                var entityDistanceToPlayer = Math.Sqrt(Math.Pow(playerPos.X - entityPos.X, 2) + Math.Pow(playerPos.Y - entityPos.Y, 2));
                bool isTargetable = Memory.ReadByte(entity.GetComponent<Targetable>().Address + 0x30) == 1;
                bool isTargeted = Memory.ReadByte(entity.GetComponent<Targetable>().Address + 0x32) == 1;

                //bool isTargeted = entity.GetComponent<Targetable>().isTargeted;

                //Doors
                if (Settings.doors)
                {
                    bool isBlacklisted = doorBlacklist != null && doorBlacklist.Contains(entity.Path);

                    if (entity.HasComponent<TriggerableBlockage>() && entity.HasComponent<Targetable>() && entity.Path.ToLower().Contains("door"))
                    {
                        bool isClosed = entity.GetComponent<TriggerableBlockage>().IsClosed;

                        string s = isClosed ? "closed" : "opened";
                        Color c = isClosed ? Color.Red : Color.Green;

                        if (!isBlacklisted)
                        {
                            Graphics.DrawText(s, 16, entityScreenPos, c, FontDrawFlags.Center);
                        }

                        if (isTargeted)
                        {
                            if (Keyboard.IsKeyPressed((int)Settings.toggleEntityKey.Value))
                            {
                                toggleDoorBlacklistItem(entity.Path);
                            }
                        }

                        if (Control.MouseButtons == MouseButtons.Left)
                        {
                            int clickCount = getEntityClickedCount(entity);
                            if (!isBlacklisted && entityDistanceToPlayer <= Settings.doorDistance && isClosed && clickCount <= 25)
                            {
                                open(entityScreenPos, prevMousePosition);
                                clickedEntities[entity.Address] = clickCount + 1;
                                if (Settings.BlockInput) Mouse.blockInput(true);
                            }
                            else if (!isBlacklisted && entityDistanceToPlayer >= Settings.doorDistance && isClosed && clickCount >= 25)
                            {
                                clickedEntities.Clear();
                            }
                        
                            if (Settings.BlockInput) Mouse.blockInput(false);


                        }
                    }
                }

                //Switches
                if (Settings.switches)
                {
                    bool isBlacklisted = switchBlacklist != null && switchBlacklist.Contains(entity.Path);

                    if (entity.HasComponent<Transitionable>() && entity.HasComponent<Targetable>() && !entity.HasComponent<TriggerableBlockage>() && entity.Path.ToLower().Contains("switch"))
                    {
                        var switchState = entity.InternalEntity.GetComponent<Transitionable>().switchState;
                        bool switched = switchState != 1;



                        string s = isTargeted ? "targeted" : "not targeted";
                        Color c = isTargeted ? Color.Green : Color.Red;

                        if (!isBlacklisted)
                        {
                            int count = 1;
                            Graphics.DrawText(s, 20, entityScreenPos.Translate(0, count * 16), c, FontDrawFlags.Center);
                            count++;
                            string s2 = switched ? "switched" : "not switched";
                            Color c2 = switched ? Color.Green : Color.Red;
                            Graphics.DrawText(s2 + ":" + switchState, 20, entityScreenPos.Translate(0, count * 16), c2, FontDrawFlags.Center);
                            count++;
                        }

                        if (isTargeted)
                        {
                            if (Keyboard.IsKeyPressed((int)Settings.toggleEntityKey.Value))
                            {
                                toggleSwitchBlacklistItem(entity.Path);
                            }
                        }

                        if (Control.MouseButtons == MouseButtons.Left)
                        {
                            int clickCount = getEntityClickedCount(entity);
                            if (!isBlacklisted && entityDistanceToPlayer <= Settings.switchDistance && !switched && clickCount <= 25)
                            {
                                open(entityScreenPos, prevMousePosition);
                                clickedEntities[entity.Address] = clickCount + 1;
                                if (Settings.BlockInput) Mouse.blockInput(true);
                            }
                            else if (!isBlacklisted && entityDistanceToPlayer >= Settings.switchDistance && !switched && clickCount >= 25)
                            {
                                clickedEntities.Clear();
                            }
                            if (Settings.BlockInput) Mouse.blockInput(false);
                        }
                    }
                }

                //Chests
                if (Settings.chests)
                {
                    if (entity.HasComponent<Chest>() || entity.Path.ToLower().Contains("chest"))
                    {
                        bool isOpened = entity.GetComponent<Chest>().IsOpened;
                        bool whitelisted = chestWhitelist != null && chestWhitelist.Contains(entity.Path);

                        if (isTargetable && !isOpened && whitelisted)
                        {
                            Graphics.DrawText("Open me!", 12, entityScreenPos, Color.LimeGreen, FontDrawFlags.Center);
                        }

                        if (isTargeted)
                        {
                            if (Keyboard.IsKeyPressed((int)Settings.toggleEntityKey.Value))
                            {
                                toggleChestWhitelistItem(entity.Path);
                            }
                        }

                        if (Control.MouseButtons == MouseButtons.Left)
                        {
                            int clickCount = getEntityClickedCount(entity);

                            if (isTargetable && whitelisted && entityDistanceToPlayer <= Settings.chestDistance && !isOpened && clickCount <= 25)
                            {
                                open(entityScreenPos, prevMousePosition);
                                clickedEntities[entity.Address] = clickCount + 1;
                                if (Settings.BlockInput) Mouse.blockInput(true);
                            }
                            else if (isTargetable && whitelisted && entityDistanceToPlayer >= Settings.chestDistance && !isOpened && clickCount >= 25)
                            {
                                clickedEntities.Clear();
                            }
                            if (Settings.BlockInput) Mouse.blockInput(false);
                        }
                    }
                }

                //Shrines
                if (Settings.shrines)
                {
                    if (entity.HasComponent<Shrine>() || entity.Path.ToLower().Contains("darkshrine"))
                    {
                        bool isOpened = entity.GetComponent<Chest>().IsOpened;
                        bool whitelisted = chestWhitelist.Contains(entity.Path);

                        if (isTargetable)
                        {
                            Graphics.DrawText("Get me!", 12, entityScreenPos, Color.LimeGreen, FontDrawFlags.Center);
                        }

                        if (Control.MouseButtons == MouseButtons.Left)
                        {
                            int clickCount = getEntityClickedCount(entity);

                            if (isTargetable && entityDistanceToPlayer <= Settings.shrineDistance && clickCount <= 25)
                            {
                                open(entityScreenPos, prevMousePosition);
                                clickedEntities[entity.Address] = clickCount + 1;
                                if (Settings.BlockInput) Mouse.blockInput(true);
                            }
                            else if (isTargetable && entityDistanceToPlayer >= Settings.shrineDistance && clickCount >= 25)
                            {
                                clickedEntities.Clear();
                            }
                            if (Settings.BlockInput) Mouse.blockInput(false);
                        }
                    }
                }
            }
        }

        private int getEntityClickedCount(EntityWrapper entity)
        {
            int clickCount = 0;

            if (clickedEntities.ContainsKey(entity.Address))
            {
                clickCount = clickedEntities[entity.Address];
            }
            else
            {
                clickedEntities.Add(entity.Address, clickCount);
            }
            if (clickCount >= 25)
            {
                LogMessage(entity.Path + " clicked too often!", 3);
            }
            return clickCount;
        }

        private void open(Vector2 entityScreenPos, Vector2 prevMousePosition)
        {
            entityScreenPos += windowOffset;
            Mouse.moveMouse(entityScreenPos);
            Mouse.LeftUp(0);
            Mouse.LeftDown(0);
            Mouse.LeftUp(0);
            Mouse.moveMouse(prevMousePosition);
            Mouse.LeftDown(0);
            Thread.Sleep(Settings.Speed);
        }



        private void loadDoorBlacklist()
        {
            try
            {
                doorBlacklist = File.ReadAllLines(PluginDirectory + "\\doorBlacklist.txt").ToList();
            }
            catch (Exception)
            {
                File.Create(PluginDirectory + "\\doorBlacklist.txt");
                loadDoorBlacklist();
            }
        }

        private void loadSwitchBlacklist()
        {
            try
            {
                switchBlacklist = File.ReadAllLines(PluginDirectory + "\\switchBlacklist.txt").ToList();
            }
            catch (Exception)
            {
                File.Create(PluginDirectory + "\\switchBlacklist.txt");
                loadSwitchBlacklist();
            }
        }

        private void loadChestWhitelist()
        {
            try
            {
                chestWhitelist = File.ReadAllLines(PluginDirectory + "\\chestWhitelist.txt").ToList();
            }
            catch (Exception)
            {
                File.Create(PluginDirectory + "\\chestWhitelist.txt");
                loadChestWhitelist();
            }
        }



        private void toggleDoorBlacklistItem(String name)
        {
            if (doorBlacklist.Contains(name))
            {
                doorBlacklist.Remove(name);
                LogMessage(name + " will now be opened", 5, Color.Green);
            }
            else
            {
                doorBlacklist.Add(name);
                LogMessage(name + " will now be ignored", 5, Color.Red);
            }
            File.WriteAllLines(PluginDirectory + "\\doorBlacklist.txt", doorBlacklist);
        }

        private void toggleSwitchBlacklistItem(String name)
        {
            if (switchBlacklist.Contains(name))
            {
                switchBlacklist.Remove(name);
                LogMessage(name + " will now be opened", 5, Color.Green);
            }
            else
            {
                switchBlacklist.Add(name);
                LogMessage(name + " will now be ignored", 5, Color.Red);
            }
            File.WriteAllLines(PluginDirectory + "\\switchBlacklist.txt", switchBlacklist);
        }

        private void toggleChestWhitelistItem(String name)
        {
            if (chestWhitelist.Contains(name))
            {
                chestWhitelist.Remove(name);
                LogMessage(name + " will now be ignored", 5, Color.Red);
            }
            else
            {
                chestWhitelist.Add(name);
                LogMessage(name + " will now be opened", 5, Color.Green);
            }
            File.WriteAllLines(PluginDirectory + "\\chestWhitelist.txt", chestWhitelist);
        }


    }

    internal class Transitionable : PoeHUD.Poe.Component
    {
        public byte switchState => M.ReadByte(Address + 0x120);
    }
}