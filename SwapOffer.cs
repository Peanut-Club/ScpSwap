using Compendium;
using Compendium.Features;
using helpers;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using helpers.Random;
using Utils.NonAllocLINQ;

namespace SCPSwap {
    public class SwapOffer {
        public readonly ReferenceHub CreatorHub;
        public Vector3 SafePosition;

        public RoleTypeId Role { get => CreatorHub.roleManager.CurrentRole.RoleTypeId; }

        public readonly OfferAvailability Availability;

        public readonly ReferenceHub TargetHub;

        public readonly bool IsAfkSwap;

        public readonly Dictionary<ReferenceHub, Vector3> Participants = new Dictionary<ReferenceHub, Vector3>();

        private readonly Stopwatch Stopwatch = Stopwatch.StartNew();


        public SwapOffer(ReferenceHub hub, OfferAvailability availability = OfferAvailability.Alive, bool isAfk = false) {
            if (hub == null) throw new ArgumentNullException(nameof(hub));
            if (!SwapManager.SafeToSwap(hub)) throw new ArgumentException($"'{hub.Nick()}' is not safely grounded");
            CreatorHub = hub;
            Availability = availability;
            IsAfkSwap = isAfk;
            SafePosition = hub.Position();
        }

        public SwapOffer(ReferenceHub hub, ReferenceHub targetHub) {
            if (hub == null) throw new ArgumentNullException(nameof(hub));
            if (targetHub == null) throw new ArgumentNullException(nameof(targetHub));
            if (!SwapManager.SafeToSwap(hub)) throw new ArgumentException($"'{hub.Nick()}' is not safely grounded");
            CreatorHub = hub;
            TargetHub = targetHub;
            Availability = OfferAvailability.Private;
            IsAfkSwap = false;
            SafePosition = hub.Position();
        }

        public bool IsAvailableForHub(ReferenceHub hub) =>
            Availability == OfferAvailability.Private
                ? TargetHub == hub
                : Availability.IsAvailableForTeam(hub.GetTeam());

        public bool CheckSwapConditions() =>
            IsTimeUp() ||
            Availability == OfferAvailability.Private && Participants.Any() ||
            (ScpSwapConfig.RouletteEnabled
                ? Participants.Count >= ScpSwapConfig.RouletteThreshold
                : Participants.Any());

        public bool IsTimeUp() => TimeRemaining() <= 0;

        public int TimeRemaining() =>
            Math.Max(0, (int)Math.Ceiling(ScpSwapConfig.SwapRequestTimeout - Stopwatch.Elapsed.TotalSeconds));


        public bool TryParticipate(ReferenceHub participant) {
            if (participant == null) throw new ArgumentNullException(nameof(participant));
            if (!IsAvailableForHub(participant) || Participants.ContainsKey(participant) || !SwapManager.SafeToSwap(participant)) return false;
            FLog.Info($"Adding Participate {participant.Nick()} to {this}");
            Participants.Add(participant, participant.Position());
            return true;
        }

        public bool RemoveParticipant(ReferenceHub participant) {
            if (participant == null) throw new ArgumentNullException(nameof(participant));
            var status = Participants.Remove(participant);
            if (status)
                FLog.Info($"Removing Participant {participant.Nick()} from {this}");
            return status;
        }

        public void UpdatePositions() {
            if (SwapManager.SafeToSwap(CreatorHub)) {
                SafePosition = CreatorHub.Position();
            }
            foreach (var hub in Participants.Keys.ToArray()) {
                if (SwapManager.SafeToSwap(hub)) {
                    Participants[hub] = hub.Position();
                }
            }
        }

        public KeyValuePair<ReferenceHub, Vector3> DrawPlayer() {
            if (Participants.IsEmpty()) throw new NullReferenceException("No Participants");
            return Participants.ElementAt(RandomGeneration.Default.GetRandom(0, Participants.Count));
        }

        public void Cancel() {
            SwapManager.SwapOffers.Remove(this);
        }

        public override string ToString() {
            return $"{nameof(SwapOffer)}(Hub={CreatorHub.Nick()},Role={Role},OfferForTeam={Availability},TimeRemaining={TimeRemaining()}s,Participants={String.Join(", ", from pair in Participants select pair.Key.Nick())},IsAfkSwap={IsAfkSwap})";
        }
    }
}
