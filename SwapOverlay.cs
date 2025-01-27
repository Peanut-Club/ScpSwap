using helpers.Attributes;
using PluginAPI.Events;
using SmartOverlays;
using Compendium;
using Compendium.Events;
using System.Collections.Generic;
using helpers.Extensions;
using Compendium.Attributes;
using Compendium.Enums;
using System.Linq;
using CentralAuth;
using static SmartOverlays.OverlayManager;
using PlayerRoles;
using Compendium.Features;
using GameCore;
using NetworkManagerUtils;
using helpers;

namespace SCPSwap {
    public class SwapOverlay : Overlay {
        private readonly int startingLine = 12;
        private readonly MessageAlign messagesAlign = MessageAlign.Right;

        private Message mainMessage = new Message("");
        private List<Message> messages = new List<Message>();

        public readonly ReferenceHub Owner;
        public static List<SwapOverlay> Overlays = new List<SwapOverlay>();

        public SwapOverlay(ReferenceHub owner) : base("Swap Overlay"/*, 15*/) {
            AddMessage(mainMessage, startingLine + 1, messagesAlign);
            Owner = owner;
        }

        [RoundStateChanged(RoundState.InProgress)]
        private static void AlertAvailableScpSwap() {
            string infoMessgae = $"Máš {ScpSwapConfig.CreateSwapTimeout} vteřin se vyměnit s";
            if (ScpSwapConfig.CreateSwapTimeout < 0) {
                infoMessgae = "Do konce kola se můžeš vyměnit s";
            }
            Calls.Delay(1f, delegate {
                var hubs = Hub.Hubs.Where(h => h.IsSCP(includeZombies: false));
                foreach (var hub in hubs) {
                    string message =
                    $"<size=65%>{infoMessgae}: (do konzole)</size>\n" +
                    $"<size=55%>" +
                        $"Jiným SCP: <color=yellow>.scpswap</color>\n" +
                        $"Kýmkoliv: <color=yellow>.scpoffer</color>\n" +
                        $"Konkrétní osobou: <color=yellow>.scpoffer <Jméno hráče></color>" +
                    $"</size>";
                    hub.AddTempHint(message, duration: 45, voffset: -11);
                }
            });
        }

        public override void UpdateMessages() {
            int lastMessageIndex = 0;
            bool canAccept = false;
            bool canCancel = false;
            foreach (var offer in SwapManager.ViewSwapOffersForHub(Owner)) {
                if (offer == null) continue;
                if (offer.CreatorHub == Owner || offer.Participants.ContainsKey(Owner)) {
                    canCancel = true;
                } else {
                    canAccept = true;
                }

                Message message = getOrAddMessage(lastMessageIndex++);

                int count = offer.Participants.Count;
                string name = $"<color=red>{offer.Role.ToString().SpaceByPascalCase().Replace(' ', '-')}:</color>";
                string part = " 0/1";
                if (offer.Availability != OfferAvailability.Private) {
                    part = ScpSwapConfig.RouletteEnabled ? $" {count}/{ScpSwapConfig.RouletteThreshold}" : " " + count.ToString();
                }
                string forTeam = $"[<color={offer.Availability.GetColor()}>{offer.Availability.GetText()}</color>]";
                string isAfk = offer.IsAfkSwap ? "(AFK) " : "";
                message.Content = $"<size=55%>{isAfk}{forTeam}{part} ({offer.TimeRemaining()} s.) {name}</size>";

                //          [Privátní] 0/1 (14 s.) SCP-173
                //  (AFK) [pro Diváky] 2/5 (14 s.) SCP-173

                //opravit:
                // idk, asi je to vše ok
                // 
            }

            if (lastMessageIndex > 0) {
                mainMessage.Content = $"<size=30%>TEST ver. 3</size> <size=70%>SCP Nabídky:</size>";
                if (canAccept) {
                    getOrAddMessage(lastMessageIndex++).Content = "<size=45%>Potvrdit: <color=yellow>.scpaccept</color> NEBO <color=yellow>.scpaccept <SCP Číslo></color></size>";
                }
                if (canCancel) {
                    getOrAddMessage(lastMessageIndex++).Content = "<size=45%>Zrušit: <color=yellow>.scpcancel</color></size>";
                }
                MadeChange();
            } else {
                mainMessage.Content = $"";
            }

            for (int lastIndex = messages.Count - 1; lastIndex >= lastMessageIndex; lastIndex--) {
                RemoveMessage(messages[lastIndex]);
                messages.RemoveAt(lastIndex);
            }
        }

        public static void UpdateAllOverlays() {
            Overlays.ForEach(overlay => overlay.UpdateMessages());
        }

        public static SwapOverlay RegisterHub(ReferenceHub hub) {
            var overlay = new SwapOverlay(hub);
            Overlays.Add(overlay);
            hub.RegisterOverlay(overlay);
            return overlay;
        }

        public static bool UnregisterHub(ReferenceHub hub, bool leaving = false) {
            if(!Overlays.TryGetFirst(overlay => overlay.Owner == hub, out var overlay) || overlay == null) {
                return false;
            }

            if (!leaving) {
                hub.UnregisterOverlay(overlay);
            }
            Overlays.Remove(overlay);
            return true;
        }

        [Load]
        public static void RegisterEvents() {
            PreDisplayEvent.Register(UpdateAllOverlays);
        }

        [Unload]
        public static void UnregisterEvents() {
            PreDisplayEvent.Unregister(UpdateAllOverlays);
        }

        [Event]
        private static void PlayerJoined(PlayerJoinedEvent ev) {
            var hub = ev.Player.ReferenceHub;
            Calls.NextFrame(() => RegisterHub(hub));
        }

        [Event]
        private static void PlayerLeft(PlayerLeftEvent ev) {
            var hub = ev.Player.ReferenceHub;
            UnregisterHub(hub, leaving: true);
        }

        private Message getOrAddMessage(int index) {
            if (index + 1 > messages.Count) {
                Message message = new Message();
                messages.Add(message);
                AddMessage(message, (float)startingLine - (0.7f * (float)index), messagesAlign);
                return message;
            }
            return messages[index];
        }
    }
}
