using EntityStates.Missions.AccessCodes.Node;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace ChangeWingStageNumber
{
    public class AccessNodeControllerNux : NetworkBehaviour
    {
        private PurchaseInteraction purchaseInteraction;
        private EntityStateMachine esm;
        private GameObject fightPrefab = Main.portalPrefab;
        private SolusFight solusFight;

        public void Start()
        {
            this.purchaseInteraction = this.GetComponent<PurchaseInteraction>();
            this.esm = this.GetComponent<EntityStateMachine>();
            GameObject fight = GameObject.Instantiate(fightPrefab, this.transform);
            solusFight = fight.transform.GetChild(0).GetComponent<SolusFight>();

            if (NetworkServer.active && this.purchaseInteraction)
            {
                purchaseInteraction.SetAvailable(true);
                purchaseInteraction.onPurchase.AddListener(OnPurchase);
            }
        }

        private void OnEnable()
        {
            TeleporterInteraction.onTeleporterBeginChargingGlobal += new Action<TeleporterInteraction>(this.TurnOffNode);
            if (!NetworkServer.active)
                return;
            Chat.SendBroadcastChat((ChatMessageBase)new Chat.SimpleChatMessage()
            {
                baseToken = "ACCESSCODES_SPAWNED"
            });
        }

        private void OnDisable()
        {
            TeleporterInteraction.onTeleporterBeginChargingGlobal -= new Action<TeleporterInteraction>(this.TurnOffNode);
        }

        [Server]
        public void TurnOffNode(TeleporterInteraction interaction)
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[Server] function 'AccessNodeControllerNux::TurnOffNode(RoR2.TeleporterInteraction)' called on client");
            }
            else
            {
                this.purchaseInteraction.SetAvailable(false);
            }
        }

        [Server]
        public void OnPurchase(Interactor interactor)
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[Server] function 'AccessNodeControllerNux::OnPurchase(RoR2.Interactor)' called on client");
            }
            else
            {
                this.purchaseInteraction.SetAvailable(false);
                this.esm.SetState(new NodeActive());
                solusFight.TriggerServer();
            }
        }
    }
}