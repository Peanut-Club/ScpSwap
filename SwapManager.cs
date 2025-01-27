using Compendium.Attributes;
using Compendium.Features;
using Compendium.Updating;
using Compendium;
using Compendium.Events;
using helpers;
using InventorySystem.Items.MicroHID;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.Spectating;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CustomPlayerEffects;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms;
using InventorySystem.Items;
using InventorySystem;
using InventorySystem.Items.Firearms.Modules;

namespace SCPSwap {
    public static class SwapManager {
        public static readonly List<SwapOffer> SwapOffers = new List<SwapOffer>();

        public static readonly Dictionary<ReferenceHub, int> SuccessfulSwaps = new Dictionary<ReferenceHub, int>();


        public static void AddOffer(SwapOffer offer) {
            FLog.Info($"Adding SwapOffer: {offer}");
            SwapOffers.Add(offer);
        }

        public static bool RemoveOffer(SwapOffer offer) {
            FLog.Info($"Removing SwapOffer: {offer}");
            return SwapOffers.Remove(offer);
        }

        public static int RemoveOffers(Predicate<SwapOffer> predicate) {
            FLog.Info("SwapOffers removed:");
            SwapOffers.Where(offer => predicate(offer)).ForEach(offer => FLog.Info("\t" + offer));
            return SwapOffers.RemoveAll(predicate);
        }

        public static bool CanCreateSwapOffer(ReferenceHub hub, out string reason) {
            if (!hub.IsSCP(includeZombies: false)) {
                reason = "You don't have SCP role.";
                return false;
            } else if (!CheckValidCreateTime()) {
                reason = $"Create SCP Swap time window has passed ({ScpSwapConfig.CreateSwapTimeout} secs).";
                return false;
            }

            return CanSwap(hub, out reason);
        }

        public static bool CanSwap(ReferenceHub hub, out string reason) {
            if (!CheckNoActiveOffer(hub)) {
                reason = "You already have Active SCP Offer.";
                return false;
            } else if (!SafeToSwap(hub)) {
                reason = "You must be on safe spot.";
                return false;
            }

            reason = "Reached Swap Limit.";
            return CheckNoReachedLimit(hub);
        }

        public static bool CheckValidCreateTime() =>
            ScpSwapConfig.CreateSwapTimeout < 0 ||
            (int)Round.Duration.TotalSeconds < ScpSwapConfig.CreateSwapTimeout;

        public static bool CheckNoReachedLimit(ReferenceHub hub) =>
            !SuccessfulSwaps.TryGetValue(hub, out int count) || count < ScpSwapConfig.MaxPlayerSwaps;

        public static bool CheckNoActiveOffer(ReferenceHub hub) =>
            !SwapOffers.TryGetFirst(req => req.CreatorHub == hub, out var _);

        public static bool SafeToSwap(ReferenceHub hub) {
            if (hub.Role() is Scp079Role || hub.Role() is SpectatorRole) {
                return true;
            }

            return hub.IsGrounded() &&
                   !CloseToTesla(hub) &&
                   Physics.Raycast(hub.Position(), Vector3.down, 2f, PlayerRolesUtils.BlockerMask);
        }

        public static bool CloseToTesla(ReferenceHub hub) {
            return TeslaGate.AllGates.TryGetFirst(tesla => tesla.IsInIdleRange(hub), out _);
        }

        public static IEnumerable<SwapOffer> ViewSwapOffersForHub(ReferenceHub hub) {
            return SwapOffers.Where(offer => offer.CreatorHub == hub || offer.IsAvailableForHub(hub));
        }


        [Update(Delay = 1000, IsUnity = true, PauseRestarting = true, PauseWaiting = true)]
        private static void CheckPendingTime() {
            for (int i = SwapOffers.Count - 1; i >= 0; i--) {
                SwapOffer offer = SwapOffers[i];
                offer.UpdatePositions();
                if (!offer.CheckSwapConditions()) continue;
                FinishSwapOffer(offer);
            }
        }

        [RoundStateChanged(Compendium.Enums.RoundState.WaitingForPlayers)]
        public static void ClearOffers() {
            SwapOffers.Clear();
        }

        [Event]
        private static void RemoveOfferOnRoleChange(PlayerChangeRoleEvent ev) {
            var hub = ev.Player.ReferenceHub;
            if (SwapOffers.TryGetFirst(offer => offer.CreatorHub == hub, out var swapOffer)) {
                swapOffer.Cancel();
            }
            foreach (var offer in SwapOffers) {
                offer.RemoveParticipant(hub);
            }
        }

        private static void AddSwapCount(ReferenceHub hub) {
            if (hub == null) return;
            if (SuccessfulSwaps.ContainsKey(hub)) {
                SuccessfulSwaps[hub] += 1;
            } else {
                SuccessfulSwaps[hub] = 1;
            }
        }

        private static void RemovePlayerFromOffers(ReferenceHub hub) {
            if (hub == null) return;
            SwapOffers.ForEach((offer) => offer.RemoveParticipant(hub));
        }

        public static bool FinishSwapOffer(SwapOffer offer) {
            if (!offer.Participants.Any()) {
                FLog.Warn($"Could not swap, offer had no Participants: {offer}");
                RemoveOffer(offer);
                return false;
            }
            try {
                var pair2 = offer.DrawPlayer();
                var hub2 = pair2.Key;
                FLog.Info($"In Offer: {offer}");
                FLog.Info($"Swapping {offer.CreatorHub.Nick()} and {hub2.Nick()}");
                SwapPlayers(offer.CreatorHub, hub2, position1: offer.SafePosition, position2: pair2.Value, preserveHealth: ScpSwapConfig.PreserveHealth);
                RemovePlayerFromOffers(hub2);
                AddSwapCount(offer.CreatorHub);
                AddSwapCount(hub2);
                ApplySwapEffect(offer.CreatorHub);
                ApplySwapEffect(hub2);
            } catch (Exception e) {
                FLog.Error("Error occured while Swapping players:");
                FLog.Error(e);
                return false;
            } finally {
                RemoveOffer(offer);
            }
            return true;
        }

        //TODO: transfer items with correct item serial
        public static void SwapPlayers(ReferenceHub hub1, ReferenceHub hub2, Vector3? position1 = null, Vector3? position2 = null, bool preserveHealth = true) {
            if (hub1 == null) throw new ArgumentNullException(nameof(hub1));
            if (hub2 == null) throw new ArgumentNullException(nameof(hub2));
            RoleTypeId role1 = hub1.RoleId();
            RoleTypeId role2 = hub2.RoleId();
            float health1 = hub1.Health();
            float health2 = hub2.Health();
            List<ItemType> items1 = (from item in hub1.inventory.UserInventory.Items select item.Value.ItemTypeId).ToList();
            List<ItemType> items2 = (from item in hub2.inventory.UserInventory.Items select item.Value.ItemTypeId).ToList();
            Dictionary<ItemType, ushort> ammo1 = hub1.inventory.UserInventory.ReserveAmmo.Copy();
            Dictionary<ItemType, ushort> ammo2 = hub2.inventory.UserInventory.ReserveAmmo.Copy();
            Vector3 pos1 = position1 ?? hub1.Position();
            Vector3 pos2 = position2 ?? hub2.Position();
            int totalExp1 = 0;
            int totalExp2 = 0;
            if (hub1.roleManager.CurrentRole is Scp079Role scp079_1 && scp079_1.SubroutineModule.TryGetSubroutine<Scp079TierManager>(out var tierManager1)) {
                totalExp1 = tierManager1.TotalExp;
            }
            if (hub2.roleManager.CurrentRole is Scp079Role scp079_2 && scp079_2.SubroutineModule.TryGetSubroutine<Scp079TierManager>(out var tierManager2)) {
                totalExp2 = tierManager2.TotalExp;
            }


            hub1.roleManager.ServerSetRole(role2, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.None);
            hub2.roleManager.ServerSetRole(role1, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.None);
            hub1.Position(pos2);
            hub2.Position(pos1);
            hub1.ClearItems();
            hub2.ClearItems();
            hub1.inventory.UserInventory.ReserveAmmo = ammo2;
            hub1.inventory.ServerSendAmmo();
            hub2.inventory.UserInventory.ReserveAmmo = ammo1;
            hub2.inventory.ServerSendAmmo();
            if (preserveHealth) {
                hub1.Health(health2);
                hub2.Health(health1);
            }
            if (hub1.roleManager.CurrentRole is Scp079Role new_scp079_1 && new_scp079_1.SubroutineModule.TryGetSubroutine<Scp079TierManager>(out var new_tierManager1)) {
                new_tierManager1.TotalExp = totalExp2;
            }
            if (hub2.roleManager.CurrentRole is Scp079Role new_scp079_2 && new_scp079_2.SubroutineModule.TryGetSubroutine<Scp079TierManager>(out var new_tierManager2)) {
                new_tierManager2.TotalExp = totalExp1;
            }
            items1.ForEach((type) => GrantItem(hub2, type));
            items2.ForEach((type) => GrantItem(hub1, type));
        }

        public static void ApplySwapEffect(ReferenceHub hub) {
            if (hub.playerEffectsController.TryGetEffect<Ensnared>(out var ensnared)) {
                ensnared.ServerSetState(1, duration: 2f);
            }
            if (hub.playerEffectsController.TryGetEffect<SpawnProtected>(out var spawnProtected)) {
                spawnProtected.ServerSetState(1, duration: 6f);
            }
        }

        public static ItemBase GrantItem(ReferenceHub ply, ItemType id) {
            ItemBase itemBase = ply.inventory.ServerAddItem(id, 0);
            if (!(itemBase is Firearm firearm)) {
                return itemBase;
            }

            if (AttachmentsServerHandler.PlayerPreferences.TryGetValue(ply, out var value) && value.TryGetValue(itemBase.ItemTypeId, out var value2)) {
                firearm.ApplyAttachmentsCode(value2, reValidate: true);
            }



            if (firearm.Modules.TryGetFirst<MagazineModule>(out var magazineModule)) {
                magazineModule.AmmoStored = magazineModule.AmmoMax;
            }

            //FirearmStatusFlags firearmStatusFlags = FirearmStatusFlags.MagazineInserted;
            //if (firearm.HasAdvantageFlag(AttachmentDescriptiveAdvantages.Flashlight)) {
            //    firearmStatusFlags |= FirearmStatusFlags.FlashlightEnabled;
            //}
            //firearm.Status = new FirearmStatus(firearm.AmmoManagerModule.MaxAmmo, firearmStatusFlags, firearm.GetCurrentAttachmentsCode());
            return itemBase;
        }
    }
}
