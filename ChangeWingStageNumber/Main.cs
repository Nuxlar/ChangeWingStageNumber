using BepInEx;
using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RoR2;
using EntityStates.Missions.AccessCodes.Node;
using R2API;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace ChangeWingStageNumber
{
  [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
  public class Main : BaseUnityPlugin
  {
    public const string PluginGUID = PluginAuthor + "." + PluginName;
    public const string PluginAuthor = "Nuxlar";
    public const string PluginName = "ChangeWingStageNumber";
    public const string PluginVersion = "1.0.0";

    internal static Main Instance { get; private set; }
    public static string PluginDirectory { get; private set; }

    private const System.Reflection.BindingFlags allFlags = (System.Reflection.BindingFlags)(-1);

    private static ConfigEntry<bool> goToMoon;
    private static ConfigFile CWSNConfig { get; set; }

    public static GameObject portalPrefab;
    private static GameObject nodePrefab;
    private static InteractableSpawnCard interactableSpawnCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();

    public void Awake()
    {
      Instance = this;

      CWSNConfig = new ConfigFile(Paths.ConfigPath + "\\com.Nuxlar.ChangeWingStageNumber.cfg", true);
      goToMoon = CWSNConfig.Bind<bool>("General", "Moon Portal", true, "Portal after Solus Heart goes to the Moon.");

      Log.Init(Logger);
      { new ILHook(typeof(Main).GetMethod(nameof(BaseStateOnEnterCaller), allFlags), BaseStateOnEnterCallerMethodModifier); }

      LoadAssets();

      On.RoR2.AccessCodesMissionController.OnStartServer += DieInAFire;
      On.EntityStates.Missions.AccessCodes.Node.AccessNodesBaseState.OnEnter += ReplaceOnEnter;
      On.EntityStates.Missions.AccessCodes.Node.NodeActive.OnEnter += KillMe;
      if (goToMoon.Value)
        On.RoR2.SolusWebMissionController.SpawnPortal += ChangeDestination;
    }

    private void KillMe(On.EntityStates.Missions.AccessCodes.Node.NodeActive.orig_OnEnter orig, EntityStates.Missions.AccessCodes.Node.NodeActive self)
    {
      if (!(bool)self.childLocator)
        self.childLocator = self.GetComponent<ChildLocator>();
      self.SwapMaterials(AccessNodesBaseState.NodeStates.on);
      GameObject effectPrefab = AccessNodesBaseState.completedEffectPrefab;
      if ((bool)effectPrefab)
      {
        Transform child = self.childLocator.FindChild(AccessNodesBaseState.effectAttachString);
        EffectData effectData = new EffectData()
        {
          origin = child.position,
          rotation = child.rotation
        };
        effectData.SetChildLocatorTransformReference(child.gameObject, 0);
        EffectManager.SpawnEffect(effectPrefab, effectData, true);
      }
      BaseStateOnEnterCaller(self);
    }

    private void ChangeDestination(On.RoR2.SolusWebMissionController.orig_SpawnPortal orig, SolusWebMissionController self, GameObject prefab, Transform spawnPoint, bool setRunNextStage)
    {
      if (prefab == null || spawnPoint == null)
        return;
      GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab, spawnPoint.position, spawnPoint.rotation);
      if (setRunNextStage)
      {
        gameObject.GetComponent<SceneExitController>().destinationScene = SceneCatalog.GetSceneDefFromSceneName("moon2");
        //gameObject.GetComponent<SceneExitController>().useRunNextStageScene = true;
      }
      NetworkServer.Spawn(gameObject);
    }

    private void ReplaceOnEnter(On.EntityStates.Missions.AccessCodes.Node.AccessNodesBaseState.orig_OnEnter orig, EntityStates.Missions.AccessCodes.Node.AccessNodesBaseState self)
    {
      Transform child = self.childLocator.FindChild("OFF");
      if ((bool)(UnityEngine.Object)child)
        self._nodeMeshAnimator = child.GetComponent<Animator>();
      //orig(self);
    }

    private void DieInAFire(On.RoR2.AccessCodesMissionController.orig_OnStartServer orig, AccessCodesMissionController self)
    {

    }

    private static void LoadAssets()
    {
      AssetReferenceT<GameObject> nodeRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_35_0.RoR2_DLC3_AccessCodesNode.Access_Codes_Node_prefab);
      AssetReferenceT<GameObject> portalRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_35_0.RoR2_DLC3_AccessCodesNode.Access_Codes_Portals_prefab);
      AssetReferenceT<GameObject> missionRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_35_0.RoR2_DLC3_AccessCodesNode.GROUP__Access_Codes_prefab);
      AssetReferenceT<ExpansionDef> expansionRef = new AssetReferenceT<ExpansionDef>(RoR2BepInExPack.GameAssetPaths.Version_1_35_0.RoR2_DLC3.DLC3_asset);

      ExpansionDef def = AssetAsyncReferenceManager<ExpansionDef>.LoadAsset(expansionRef).WaitForCompletion();
      GameObject portals = AssetAsyncReferenceManager<GameObject>.LoadAsset(portalRef).WaitForCompletion();
      portalPrefab = portals;

      AssetAsyncReferenceManager<GameObject>.LoadAsset(nodeRef).Completed += (x) =>
      {
        GameObject origPrefab = x.Result;
        nodePrefab = PrefabAPI.InstantiateClone(origPrefab, "AccessNodeNux", true);

        GameObject.Destroy(nodePrefab.GetComponent<ProxyInteraction>());
        GameObject.Destroy(nodePrefab.GetComponent<AccessCodesNodeController>());
        GameObject.Destroy(nodePrefab.GetComponent<EventLockableInteractable>());

        nodePrefab.AddComponent<ExpansionRequirementComponent>().requiredExpansion = def;

        nodePrefab.GetComponent<EntityStateMachine>().initialStateType = new EntityStates.SerializableEntityStateType(typeof(EntityStates.Missions.AccessCodes.Node.NodesOnAndReady));

        PurchaseInteraction interaction = nodePrefab.AddComponent<PurchaseInteraction>();
        interaction.contextToken = "ACCESSCODES_NAME";
        interaction.NetworkdisplayNameToken = "ACCESSCODES_NAME";
        nodePrefab.AddComponent<AccessNodeControllerNux>();

        Highlight highlight = nodePrefab.GetComponent<Highlight>();
        highlight.targetRenderer = nodePrefab.transform.Find("Model/AccessCodeNodeModel/mdlAccessCodesNode/meshBody").GetComponent<SkinnedMeshRenderer>();

        interactableSpawnCard.name = "iscPointOfInterestTheseNutsOnYourChin";
        interactableSpawnCard.prefab = nodePrefab;
        interactableSpawnCard.sendOverNetwork = true;
        // The size of the interactable, there's Human, Golem, and BeetleQueen
        interactableSpawnCard.hullSize = HullClassification.Golem;
        // Which nodegraph should it spawn on, air or ground
        interactableSpawnCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
        interactableSpawnCard.requiredFlags = RoR2.Navigation.NodeFlags.None;
        // Nodes have flags that help define what can be spawned on it, any node marked "NoShrineSpawn" shouldn't spawn our shrine on it
        interactableSpawnCard.forbiddenFlags = RoR2.Navigation.NodeFlags.NoShrineSpawn;
        // How much should it cost the director to spawn your interactable
        interactableSpawnCard.directorCreditCost = 0;
        interactableSpawnCard.occupyPosition = true;
        interactableSpawnCard.orientToFloor = true;
        interactableSpawnCard.skipSpawnWhenSacrificeArtifactEnabled = false;
        interactableSpawnCard.maxSpawnsPerStage = 1;

        DirectorCard directorCard = new DirectorCard
        {
          selectionWeight = 1000, // The higher this number the more common it'll be, for reference a normal chest is about 230
          spawnCard = interactableSpawnCard,
        };

        DirectorAPI.DirectorCardHolder directorCardHolder = new DirectorAPI.DirectorCardHolder
        {
          Card = directorCard,
          InteractableCategory = DirectorAPI.InteractableCategory.Chests,
        };
        // Registers the interactable on every stage
        // DirectorAPI.Helpers.AddNewInteractable(directorCardHolder);

        // Or create your stage list and register it on each of those stages
        List<DirectorAPI.Stage> stageList = new List<DirectorAPI.Stage>();

        stageList.Add(DirectorAPI.Stage.SkyMeadow);
        stageList.Add(DirectorAPI.Stage.HelminthHatchery);

        foreach (DirectorAPI.Stage stage in stageList)
        {
          DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, stage);
        }
      };
    }

    // self is just for being able to call self.OnEnter() inside hooks.
    private static void BaseStateOnEnterCaller(EntityStates.Missions.AccessCodes.Node.AccessNodesBaseState self)
    {

    }

    // self is just for being able to call self.OnEnter() inside hooks.
    private static void BaseStateOnEnterCallerMethodModifier(ILContext il)
    {
      var cursor = new ILCursor(il);
      cursor.Emit(OpCodes.Ldarg_0);
      cursor.Emit(OpCodes.Call, typeof(EntityStates.Missions.AccessCodes.Node.AccessNodesBaseState).GetMethod(nameof(EntityStates.Missions.AccessCodes.Node.AccessNodesBaseState.OnEnter), allFlags));
    }
  }
}