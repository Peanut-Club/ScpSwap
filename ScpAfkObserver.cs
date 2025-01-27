using System.Collections.Generic;
using Compendium.Updating;
using helpers.Attributes;
using PlayerRoles;
using Utils.NonAllocLINQ;

namespace SCPSwap {
    
    public static class ScpAfkObserver {
        public static readonly List<ScpAfkData> ScpAfkDataList = new List<ScpAfkData>();

        [Load]
        private static void RegisterEvents() {
            PlayerRoleManager.OnRoleChanged += RoleChange;
        }

        [Unload]
        private static void UnregisterEvents() {
            PlayerRoleManager.OnRoleChanged -= RoleChange;
        }
        
        private static void RoleChange(ReferenceHub hub, PlayerRoleBase oldRole, PlayerRoleBase newRole) {
            if (newRole.Team != Team.SCPs || newRole.RoleTypeId == RoleTypeId.Scp0492) {
                ScpAfkDataList.RemoveAll(data => data.Hub == hub);
            } else if (ScpAfkDataList.TryGetFirst(data => data.Hub == hub, out var data)) {
                data.Restart();
            } else {
                ScpAfkDataList.Add(new(hub));
            }
        }

        [Update(Delay = 1000, IsUnity = true, PauseRestarting = true, PauseWaiting = true)]
        private static void OnFixedUpdate() {
            foreach (var data in ScpAfkDataList) {
                data.Update();
            }
        }
    }
    
}