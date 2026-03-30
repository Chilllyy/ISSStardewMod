using System.Runtime.CompilerServices;
using com.lightstreamer.client;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace ChillyStardewMod
{
    internal sealed class ModEntry : Mod
    {
        public static IMonitor mon;
        public static IModHelper hel;
        private static string assetPath;
        public static string uniqueID;
        
        private static LightstreamerClient lsClient;
        public static int ISSValue = -1;

        public override void Entry(IModHelper helper)
        {
            lsClient = new LightstreamerClient("https://push.lightstreamer.com", "ISSLIVE");
            lsClient.connect();

            Subscription sub = new Subscription("MERGE", new string[] { "NODE3000005" }, new string[] { "Value" });
            sub.RequestedSnapshot = "yes";
            sub.addListener(new ISSListener());

            mon = Monitor;
            hel = Helper;
            uniqueID = ModManifest.UniqueID;
            Helper.ModContent.Load<Texture2D>("assets/ISS.png"); //Load content
            assetPath = Helper.ModContent.GetInternalAssetName("assets/ISS.png").BaseName;
            
            lsClient.subscribe(sub);
            
            var harmony = new Harmony(this.ModManifest.UniqueID);

            harmony.Patch(
                original: AccessTools.Method(typeof(TV), nameof(TV.checkForAction)),
                prefix: new HarmonyMethod(typeof(TVPatches), nameof(TVPatches.checkForAction_Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(TV), nameof(TV.selectChannel)),
                prefix: new HarmonyMethod(typeof(TVPatches), nameof(TVPatches.selectChannel_Prefix))
            );
                
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;

            helper.ConsoleCommands.Add("issosd", "Sets SS value", this.setValue);
        }

        public void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            Monitor.Log("Received Mod Message", LogLevel.Alert);
            if (e.FromModID == uniqueID)
            {
                switch (e.Type)
                {
                    case "RequestISSTrophy":
                        if (!Context.IsMainPlayer)
                        {
                            return;
                        }
                        
                        ModData data = Helper.Data.ReadSaveData<ModData>("data") ?? new ModData();

                        long farmerID = e.ReadAs<long>();
                        Farmer f = Game1.GetPlayer(farmerID);
                        if (data.claimed_trophy.Contains(f.Name))
                        {
                            return;
                        }
                        hel.Multiplayer.SendMessage(farmerID, "GiveISSTrophy", modIDs: new[] {uniqueID}, playerIDs: new[] {e.FromPlayerID});
                        data.claimed_trophy.Add(f.Name);
                        Helper.Data.WriteSaveData<ModData>("data", data);
                        break;
                    case "GiveISSTrophy":
                        long farmerId = e.ReadAs<long>();
                        Farmer farmer = Game1.GetPlayer(farmerId);
                        Item trophy = ItemRegistry.Create("ChillyTestMod_Trophy");
                        Item res = farmer.addItemToInventory(trophy);

                        int slot = farmer.getIndexOfInventoryItem(trophy);
                        if (slot <= 11)
                        {
                            farmer.CurrentToolIndex = slot;
                        }
                        break;
                }
            }
        }

        private void setValue(string command, string[] args)
        {
            try
            {
                ISSValue = int.Parse(args[0]);
                this.Monitor.Log($"Successfully set ISS value to {ISSValue}", LogLevel.Info);
            }
            catch (FormatException e)
            {
                this.Monitor.Log($"Unable to parse args as a number \n{args[0]}\n{e}", LogLevel.Alert);
            }
        }


        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            // print button presses to the console window
            this.Monitor.Log($"{Game1.player.Name} pressed {e.Button}.", LogLevel.Debug);
        }

        internal class TVPatches
        {
            internal static bool checkForAction_Prefix(TV __instance, Farmer who, bool justCheckingForActivity = false)
            {
                try
                {
                    if (justCheckingForActivity)
                    {
                        return false;
                    }
                    
                    List<Response> channels = new List<Response>();
                    channels.Add(new Response("Weather", Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13105")));
                    channels.Add(new Response("Fortune", Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13107")));
                    switch (Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth))
                    {
                        case "Mon":
                        case "Thu":
                            channels.Add(new Response("Livin'", Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13111")));
                            break;
                        case "Sun":
                            channels.Add(new Response("The", Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13114")));
                            break;
                        case "Wed":
                            if (Game1.stats.DaysPlayed > 7)
                            {
                                channels.Add(new Response("The", Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13117")));
                            }
                            break;
                    }
                    if (Game1.Date.Season == Season.Fall && Game1.Date.DayOfMonth == 26 && Game1.stats.Get("childrenTurnedToDoves") != 0 && !who.mailReceived.Contains("cursed_doll"))
                    {
                        channels.Add(new Response("???", "???"));
                    }
                    if (Game1.player.mailReceived.Contains("pamNewChannel"))
                    {
                        channels.Add(new Response("Fishing", Game1.content.LoadString("Strings\\StringsFromCSFiles:TV_Fishing_Channel")));
                    }
                    channels.Add(new Response("Space", "Space Station Status"));
                    channels.Add(new Response("(Leave)", Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13118")));
                    Game1.currentLocation.createQuestionDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13120"), channels.ToArray(), __instance.selectChannel);
                    Game1.player.Halt();
                    return false;
                }
                catch (Exception ex)
                {
                    mon.Log($"Run into issue while catching TV checkForAction Call\n{ex}", LogLevel.Alert);
                    return true;
                }
            }
            
            internal static bool selectChannel_Prefix(ref TemporaryAnimatedSprite ___screen, TV __instance, Farmer who, string answer)
            {
                try
                {
                    switch (ArgUtility.SplitBySpaceAndGet(answer, 0))
                    {
                        case "Space":
                            ___screen = new TemporaryAnimatedSprite(assetPath, new Rectangle(0, 0, 42, 28), 600f, 2, 999999, __instance.getScreenPosition(), flicker: false, flipped: false, (float)(__instance.boundingBox.Bottom - 1) / 10000f + 1E-05f, 0f, Color.White, __instance.getScreenSizeModifier(), 0f, 0f, 0f);
                            string message = "We don't have any data from the space station yet, check again later!";
                            if (ISSValue >= 0)
                            {
                                message = $"Current Urine Tank Level: {ISSValue}%";
                            }
                            if (ISSValue == 50)
                            {
                                message = "Such a momentous occasion! You hit the jackpot!";
                            }


                            Game1.drawObjectDialogue(Game1.parseText(message));
                            if (ISSValue == 50)
                            {
                                bool give_trophy = false;
                                bool host = false;
                                ModData model = null;
                                if (Context.IsMainPlayer)
                                {
                                    //Single player or we are the main host, doesn't matter much for our use case
                                    host = true;
                                    model = hel.Data.ReadSaveData<ModData>("data") ?? new ModData();
                                    give_trophy = !model.claimed_trophy.Contains(who.Name);
                                } else
                                {
                                    //we are not main player (only happens in multiplayer)
                                    if (who.freeSpotsInInventory() >= 1)
                                    {
                                        hel.Multiplayer.SendMessage(who.UniqueMultiplayerID, "RequestISSTrophy",
                                            modIDs: new[] { uniqueID });
                                    }
                                }
                                
                                Game1.afterDialogues = () =>
                                {
                                    message = "The ISS Piss tank is at 50%!!!";
                                    Game1.drawObjectDialogue(Game1.parseText(message));
                                    who.playNearbySoundLocal("achievement");
                                    if (!give_trophy)
                                    {
                                        Game1.afterDialogues = __instance.turnOffTV;
                                        return;
                                    }

                                    Item trophy = ItemRegistry.Create("ChillyTestMod_Trophy");
                                    Item res = who.addItemToInventory(trophy);
                                    if (res != null)
                                    {
                                        Game1.afterDialogues = () =>
                                        {
                                            message =
                                                "Unfortunately, you don't have enough space in your inventory, drop something and try again!";
                                            Game1.drawObjectDialogue(Game1.parseText(message));
                                            Game1.afterDialogues = __instance.turnOffTV;
                                        };
                                    }
                                    else
                                    {
                                        int slotindex = who.getIndexOfInventoryItem(trophy);
                                        if (slotindex <= 11)
                                        {
                                            who.CurrentToolIndex = slotindex;
                                        }

                                        Game1.afterDialogues = __instance.turnOffTV;
                                        if (give_trophy)
                                        {
                                            if (host)
                                            {
                                                model.claimed_trophy.Add(who.Name);
                                                hel.Data.WriteSaveData("data", model);
                                            }
                                        }
                                    }
                                };
                            }
                            else
                            {
                                Game1.afterDialogues = __instance.turnOffTV;
                            }
                            return false;
                    }
                }
                catch (Exception ex)
                { 
                    mon.Log($"Error while playing custom channel text! {ex}", LogLevel.Alert);
                }

                return true;
            }
        }
    }
}