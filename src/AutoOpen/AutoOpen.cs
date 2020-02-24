﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared.Enums;
using SharpDX;
using ExileCore.Shared.Helpers;
using System.IO;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using System.Collections;
using AutoOpen.Utils;

namespace AutoOpen
{
    public class AutoOpen : BaseSettingsPlugin<Settings>
    {
        private IngameState ingameState;
        private Dictionary<long, int> clickedEntities = new Dictionary<long, int>();
        private List<Entity> entities = new List<Entity>();
        private Vector2 windowOffset = new Vector2();
        private List<String> doorBlacklist;
        private List<String> switchBlacklist;
        private List<String> chestWhitelist;
        private Coroutine CoroutineWorker;
        private const string coroutineName = "AutoOpen";

        public AutoOpen()
        {
        }

        public override bool Initialise()
        {
            base.Initialise();
            Name = "AutoOpen";

            ingameState = GameController.Game.IngameState;
            windowOffset = GameController.Window.GetWindowRectangle().TopLeft;
            loadDoorBlacklist();
            loadSwitchBlacklist();
            loadChestWhitelist();

            Input.RegisterKey(Settings.toggleEntityKey.Value);

            Settings.toggleEntityKey.OnValueChanged += () => { Input.RegisterKey(Settings.toggleEntityKey.Value); };

            return true;
        }

        public override void Render()
        {
            if (!Settings.Enable) return;
            open();
        }

        public override void EntityAddedAny(Entity entity)
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

        public override void EntityRemoved(Entity entityWrapper)
        {
            base.EntityRemoved(entityWrapper);
            entities.Remove(entityWrapper);
        }

        public override void OnClose()
        {
            base.OnClose();
        }

        private IEnumerator open()
        {
            var camera = ingameState.Camera;
            var playerPos = GameController.Player.Pos;
            var prevMousePosition = Input.ForceMousePosition;


            foreach (Entity entity in entities)
            {
                if (entity.HasComponent<Targetable>() &&
                    entity.IsValid &&
                    entity.IsTargetable)
                {
                    var entityPos = entity.Pos;
                    var entityScreenPos = camera.WorldToScreen(entityPos.Translate(0, 0, 0));
                    var entityDistanceToPlayer = Math.Sqrt(Math.Pow(playerPos.X - entityPos.X, 2) + Math.Pow(playerPos.Y - entityPos.Y, 2));

                    //bool isTargetable = Memory.ReadByte(entity.GetComponent<Targetable>().Address + 0x30) == 1;
                    //bool isTargeted = Memory.ReadByte(entity.GetComponent<Targetable>().Address + 0x32) == 1;

                    bool isTargetable = entity.GetComponent<Targetable>().isTargetable;
                    bool isTargeted = entity.GetComponent<Targetable>().isTargeted;

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
                                Graphics.DrawText(s, entityScreenPos, c, FontAlign.Center);
                            }

                            if (isTargeted)
                            {
                                if (Settings.toggleEntityKey.PressedOnce())
                                {
                                    toggleDoorBlacklistItem(entity.Path);
                                }
                            }

                            if (Control.MouseButtons == MouseButtons.Left)
                            {
                                int clickCount = getEntityClickedCount(entity);
                                if (!isBlacklisted && entityDistanceToPlayer <= Settings.doorDistance && isClosed && clickCount <= 15)
                                {
                                    yield return open(entityScreenPos, prevMousePosition);
                                    clickedEntities[entity.Address] = clickCount + 1;
                                    if (Settings.BlockInput) Mouse.blockInput(true);
                                }
                                else if (!isBlacklisted && entityDistanceToPlayer >= Settings.doorDistance && isClosed && clickCount >= 15)
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
                            var switchState = entity.GetComponent<Transitionable>().Flag1;
                            bool switched = switchState != 1;

                            string s = isTargeted ? "targeted" : "not targeted";
                            Color c = isTargeted ? Color.Green : Color.Red;

                            if (!isBlacklisted)
                            {
                                int count = 1;
                                Graphics.DrawText(s, entityScreenPos.Translate(0, count * 16), c, FontAlign.Center);
                                count++;
                                string s2 = switched ? "switched" : "not switched";
                                Color c2 = switched ? Color.Green : Color.Red;
                                Graphics.DrawText(s2 + ":" + switchState, entityScreenPos.Translate(0, count * 16), c2, FontAlign.Center);
                                count++;
                            }

                            if (isTargeted)
                            {
                                if (Settings.toggleEntityKey.PressedOnce())
                                {
                                    toggleSwitchBlacklistItem(entity.Path);
                                }
                            }

                            if (Control.MouseButtons == MouseButtons.Left)
                            {
                                int clickCount = getEntityClickedCount(entity);
                                if (!isBlacklisted && entityDistanceToPlayer <= Settings.switchDistance && !switched && clickCount <= 15)
                                {
                                    yield return open(entityScreenPos, prevMousePosition);
                                    clickedEntities[entity.Address] = clickCount + 1;
                                    if (Settings.BlockInput) Mouse.blockInput(true);
                                }
                                else if (!isBlacklisted && entityDistanceToPlayer >= Settings.switchDistance && !switched && clickCount >= 15)
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
                                Graphics.DrawText("Open me!", entityScreenPos, Color.LimeGreen, FontAlign.Center);
                            }

                            if (isTargeted)
                            {
                                if (Settings.toggleEntityKey.PressedOnce())
                                {
                                    toggleChestWhitelistItem(entity.Path);
                                }
                            }

                            if (Control.MouseButtons == MouseButtons.Left)
                            {
                                int clickCount = getEntityClickedCount(entity);

                                if (isTargetable && whitelisted && entityDistanceToPlayer <= Settings.chestDistance && !isOpened && clickCount <= 15)
                                {
                                    yield return open(entityScreenPos, prevMousePosition);
                                    clickedEntities[entity.Address] = clickCount + 1;
                                    if (Settings.BlockInput) Mouse.blockInput(true);
                                }
                                else if (isTargetable && whitelisted && entityDistanceToPlayer >= Settings.chestDistance && !isOpened && clickCount >= 15)
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
                            bool isAvailable = entity.GetComponent<Shrine>().IsAvailable;
                            bool whitelisted = chestWhitelist.Contains(entity.Path);

                            if (isTargetable)
                            {
                                Graphics.DrawText("Get me!", entityScreenPos, Color.LimeGreen, FontAlign.Center);
                            }

                            if (Control.MouseButtons == MouseButtons.Left)
                            {
                                int clickCount = getEntityClickedCount(entity);

                                if (isTargetable && entityDistanceToPlayer <= Settings.shrineDistance && clickCount <= 15)
                                {
                                    yield return open(entityScreenPos, prevMousePosition);
                                    clickedEntities[entity.Address] = clickCount + 1;
                                    if (Settings.BlockInput) Mouse.blockInput(true);
                                }
                                else if (isTargetable && entityDistanceToPlayer >= Settings.shrineDistance && clickCount >= 15)
                                {
                                    clickedEntities.Clear();
                                }
                                if (Settings.BlockInput) Mouse.blockInput(false);
                            }
                        }
                    }
                }
            }

            yield break;
        }

        private int getEntityClickedCount(Entity entity)
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
            if (clickCount >= 15)
            {
                LogMessage(entity.Path + " clicked too often!", 3);
            }
            return clickCount;
        }

        private IEnumerator open(Vector2 entityScreenPos, Vector2 prevMousePosition)
        {
            entityScreenPos += windowOffset;
            yield return Input.SetCursorPositionSmooth(entityScreenPos);
            Input.Click(MouseButtons.Left);
            yield return Input.SetCursorPositionSmooth(prevMousePosition);
            yield return new WaitTime(Settings.Speed);

            yield break;
        }



        private void loadDoorBlacklist()
        {
            try
            {
                doorBlacklist = File.ReadAllLines(DirectoryFullName + "\\doorBlacklist.txt").ToList();
            }
            catch (Exception)
            {
                File.Create(DirectoryFullName + "\\doorBlacklist.txt");
                loadDoorBlacklist();
            }
        }

        private void loadSwitchBlacklist()
        {
            try
            {
                switchBlacklist = File.ReadAllLines(DirectoryFullName + "\\switchBlacklist.txt").ToList();
            }
            catch (Exception)
            {
                File.Create(DirectoryFullName + "\\switchBlacklist.txt");
                loadSwitchBlacklist();
            }
        }

        private void loadChestWhitelist()
        {
            try
            {
                chestWhitelist = File.ReadAllLines(DirectoryFullName + "\\chestWhitelist.txt").ToList();
            }
            catch (Exception)
            {
                File.Create(DirectoryFullName + "\\chestWhitelist.txt");
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
            File.WriteAllLines(DirectoryFullName + "\\doorBlacklist.txt", doorBlacklist);
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
            File.WriteAllLines(DirectoryFullName + "\\switchBlacklist.txt", switchBlacklist);
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
            File.WriteAllLines(DirectoryFullName + "\\chestWhitelist.txt", chestWhitelist);
        }

        public override void AreaChange(AreaInstance area)
        {
            entities = new List<Entity>();
            base.AreaChange(area);
        }
    }

}