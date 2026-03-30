using com.lightstreamer.client;
using StardewModdingAPI;

namespace ChillyStardewMod;

public class ISSListener : SubscriptionListener
{
    public void onClearSnapshot(string itemName, int itemPos)
    {
    }

    public void onCommandSecondLevelItemLostUpdates(int lostUpdates, string key)
    {
    }

    public void onCommandSecondLevelSubscriptionError(int code, string message, string key)
    {
    }

    public void onEndOfSnapshot(string itemName, int itemPos)
    {
    }

    public void onItemLostUpdates(string itemName, int itemPos, int lostUpdates)
    {
    }

    public void onItemUpdate(ItemUpdate itemUpdate)
    {
        string newValue = itemUpdate.getValue("Value");
        ModEntry.mon.Log($"Received New ISS Value: {newValue}", LogLevel.Debug);
        try
        {
            int val = int.Parse(newValue);
            ModEntry.ISSValue = val;
        }
        catch (ArgumentException e)
        {
            ModEntry.mon.Log($"Value received from Lightstreamer is null\n{newValue}\n{e}", LogLevel.Alert);
        }
        catch (FormatException e)
        {
            ModEntry.mon.Log($"Value received from Lightstreamer is unable to be parsed as a float\n{newValue}\n{e}", LogLevel.Alert);
        }
    }

    public void onListenEnd()
    {
    }

    public void onListenStart()
    {
    }

    public void onSubscription()
    {
    }

    public void onSubscriptionError(int code, string message)
    {
    }

    public void onUnsubscription()
    {
    }

    public void onRealMaxFrequency(string frequency)
    {
    }
}