// Assets/Scripts/Currency/WalletDebug.cs
using UnityEditor;
using UnityEngine;


[DefaultExecutionOrder(-100)]
public static class WalletDebug
{
    [MenuItem("Tools/Wallet/Delete Saved Gold")]
    public static void DeleteSavedGold()
    {
        PlayerPrefs.DeleteKey("PLAYER_GOLD");
        Debug.Log("[Wallet] Deleted PLAYER_GOLD key");
    }
}
