using BepInEx;
using RoR2.ContentManagement;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace NewMod
{
  [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
  public class Main : BaseUnityPlugin
  {
    public const string PluginGUID = PluginAuthor + "." + PluginName;
    public const string PluginAuthor = "Nuxlar";
    public const string PluginName = "NewMod";
    public const string PluginVersion = "1.0.0";

    internal static Main Instance { get; private set; }
    public static string PluginDirectory { get; private set; }

    public void Awake()
    {
      Instance = this;

      Log.Init(Logger);
      // LoadAssets();

      // PluginDirectory = Path.GetDirectoryName(Info.Location);
    }

    private static void LoadAssets()
    {
      // Example for how to properly load in assets to be used later
      // AssetAsyncReferenceManager<Material>.LoadAsset(new AssetReferenceT<Material>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_moon.matMoonTerrain_mat)).Completed += (x) => variableName = x.Result;
    }

    /*
        private static void TweakAssets()
        {
          Example for how to edit an asset once it finishes loading
          AssetAsyncReferenceManager<GameObject>.LoadAsset(new AssetReferenceT<Material>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC2_Items_LowerPricedChests.PickupSaleStar_prefab)).Completed += delegate (AsyncOperationHandle<GameObject> obj)
          {
            MeshCollider collider = obj.Result.transform.find("SaleStar")?.GetComponent<MeshCollider>();
            if (collider)
            {
              collider.convex = true;
            }
          };
        }
    */
  }
}