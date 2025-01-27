using Compendium;
using Compendium.Features;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using SmartOverlays;
using System.Diagnostics;
using UnityEngine;

namespace SCPSwap {
    public class ScpAfkData {
        public ReferenceHub Hub { get; private set; }
        public Vector3 SafePosition { get; private set; }
        public SwapOffer Offer { get; private set; }

        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        private bool warningAnnounced = false;
        private Vector3 lastPos;
        private TempMessage warningHint = null;

        public ScpAfkData(ReferenceHub hub) {
            Hub = hub;
            SafePosition = hub.Position();
            lastPos = Vector3.zero;
            _ = IsAfk();
        }

        public void Update() {
            try {
                if (SwapManager.SafeToSwap(Hub)) {
                    SafePosition = Hub.Position();
                }

                /* || !(Hub.roleManager.CurrentRole is IAFKRole iAFKRole)*/

                if (!IsAfk()) {
                    Restart();
                } else if (stopwatch.Elapsed.TotalSeconds >= ScpSwapConfig.AfkSwapTime) {
                    if (!SwapManager.CheckNoActiveOffer(Hub)) {
                        return;
                    }
                    Offer = CreateOffer();
                    SwapManager.AddOffer(Offer);
                } else if (stopwatch.Elapsed.TotalSeconds >= ScpSwapConfig.AfkSwapWarningTime && !warningAnnounced) {
                    warningAnnounced = true;
                    warningHint = Hub.AddTempHint(
                        message: ScpSwapConfig.AfkSwapWarningMessage,
                        duration: ScpSwapConfig.AfkSwapTime - ScpSwapConfig.AfkSwapWarningTime,
                        voffset: -2,
                        align: MessageAlign.Center);
                }
            } catch { };
        }

        public void Restart() {
            stopwatch.Restart();

            if (warningHint != null) {
                warningHint.SetExpired();
                warningHint = null;
            }
            warningAnnounced = false;

            if (Offer != null) {
                SwapManager.RemoveOffer(Offer);
                Offer = null;
            }
        }

        private SwapOffer CreateOffer() {
            SwapOffer afkOffer = new(Hub, OfferAvailability.Spectators, isAfk: true);
            afkOffer.SafePosition = SafePosition;
            return afkOffer;
        }

        private bool IsAfk() {
            Vector3 currentPos;
            if (Hub.roleManager.CurrentRole is Scp079Role scp079) {
                currentPos = scp079.CurrentCamera.CameraPosition;
                currentPos.x += scp079.CurrentCamera.HorizontalRotation / 20;
                currentPos.y += scp079.CurrentCamera.VerticalRotation / 20;
            } else {
                currentPos = Hub.Position();
            }

            if (lastPos == Vector3.zero) {
                lastPos = currentPos;
                return true;
            }

            if ((lastPos - currentPos).sqrMagnitude < 1.25f) {
                return true;
            }

            lastPos = currentPos;
            return false;
        }
    }
}
