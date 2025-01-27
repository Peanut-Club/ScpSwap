using BetterCommands;
using helpers;
using PlayerRoles;
using PluginAPI.Core;
using System;
using System.Linq;

namespace SCPSwap {
    public static class SwapCommands {
        [Command("scpswap", CommandType.PlayerConsole)]
        [Description("Allows you to swap SCP role with another SCP. (or Specify Player name to make private offer.)")]
        public static string ScpSwapCmd(Player player, Player target = null) {
            return TryCreateOffer(player, OfferAvailability.SCPs, target);
        }

        [Command("scpoffer", CommandType.PlayerConsole)]
        [Description("Allows you to offer SCP role to humans. (or Specify Player name to make private offer.)")]
        public static string ScpOfferCmd(Player player, Player target = null) {
            return TryCreateOffer(player, OfferAvailability.Humans, target);
        }

        public static string TryCreateOffer(Player player, OfferAvailability availability, Player target = null) {
            if (!SwapManager.CanCreateSwapOffer(player.ReferenceHub, out string reason)) {
                return "Cannot create SCP Offer: " + reason;
            }
            SwapManager.AddOffer(
                target != null
                ? new(player.ReferenceHub, target.ReferenceHub)
                : new(player.ReferenceHub, availability)
            );
            return "Successfully offered your SCP role.";
        }

        [Command("scpcancel", CommandType.PlayerConsole)]
        [Description("Cancel your SCP Offer and Removes you from registered Offers")]
        public static string ScpCancelCmd(Player player) {
            if (SwapManager.RemoveOffers(offer => offer.CreatorHub == player.ReferenceHub) > 0) {
                return "Your SCP Offer was Cancelled.";
            }

            string roles = "";
            foreach (var offer in SwapManager.SwapOffers) {
                if (!offer.RemoveParticipant(player.ReferenceHub)) continue;
                roles += ", " + offer.Role;
            }
            roles.Trim([',', ' ']);
            if (roles == "") return "You were not registered anywhere, Nothing changed.";
            return $"You were removed from Roulettes for '{roles}'.";
        }


        [Command("scpaccept", CommandType.PlayerConsole)]
        [Description("Accepts SCP Swap/Offer Request (Specify role name to participate only in specified Roulette)")]
        public static string ScpAcceptCmd(Player player, string scp_number = null) {
            if (!SwapManager.CanSwap(player.ReferenceHub, out string reason)) {
                return "Cannot accept SCP Offer: " + reason;
            }

            if (string.IsNullOrEmpty(scp_number)) {
                string roles = "";
                foreach (var offer in SwapManager.SwapOffers) {
                    if (!offer.TryParticipate(player.ReferenceHub)) continue;
                    roles += ", " + offer.Role;
                }
                roles.Trim([',', ' ']);
                if (roles == "") return "No active SCP Offers for your Team.";
                return $"Succesfully registered to Roulettes for '{roles}'.";
            }

            string only_numbers = string.Concat(scp_number.Where(Char.IsDigit));
            if (!TryGetEnumByPartialName<RoleTypeId>(only_numbers, out RoleTypeId roleType)) {
                return $"Could not parse '{scp_number}' to RoleTypeId.";
            }

            if (!SwapManager.SwapOffers.TryGetFirst(offer => offer.Role == roleType, out var singleOffer)) {
                return $"No active SCP Offers for role '{roleType}'.";
            }

            if (!singleOffer.TryParticipate(player.ReferenceHub)) {
                if (!singleOffer.Availability.IsAvailableForTeam(player.Team)) {
                    return $"Offer for {singleOffer.Role} is only available for {singleOffer.Availability}.";
                }
                return $"Already in SCP Roulette for '{singleOffer.Role}'.";
            }

            return $"Succesfully registered to Roulette for '{singleOffer.Role}'.";
        }

        public static bool TryGetEnumByPartialName<TEnum>(string name, out TEnum result) where TEnum : struct {
            foreach (string str_name in Enum.GetNames(typeof(TEnum))) {
                if (!str_name.Contains(name)) continue;
                result = (TEnum)Enum.Parse(typeof(TEnum), str_name);
                return true;
            }
            result = default(TEnum);
            return false;
        }


        [Command("grounded", CommandType.PlayerConsole)]
        [Description("Check if you are properly grounded")]
        public static string GroundedTestCmd(Player player) {
            if (!SwapManager.SafeToSwap(player.ReferenceHub)) {
                return "not grounded or close to tesla";
            }
            return "grounded";
        }
    }
}
