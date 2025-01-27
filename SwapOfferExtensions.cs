using PlayerRoles;

namespace SCPSwap {
    public static class SwapOfferExtensions {
        public static bool IsAvailableForTeam(this OfferAvailability availability, Team team) {
            switch (availability) {
                case OfferAvailability.All:
                    return team != Team.OtherAlive;
                case OfferAvailability.Alive:
                    return team != Team.Dead && team != Team.OtherAlive;
                case OfferAvailability.Humans:
                    return team != Team.Dead && team != Team.OtherAlive && team != Team.SCPs;
                case OfferAvailability.SCPs:
                    return team == Team.SCPs;
                case OfferAvailability.Spectators:
                    return team == Team.Dead;
                default: return false;
            }
        }

        public static string GetColor(this OfferAvailability availability) {
            switch (availability) {
                case OfferAvailability.All:
                    return "white";
                case OfferAvailability.Alive:
                    return "green";
                case OfferAvailability.Humans:
                    return "orange";
                case OfferAvailability.SCPs:
                    return "red";
                case OfferAvailability.Spectators:
                    return "#858585";
                case OfferAvailability.Private:
                    return "yellow";
                default: return "white";
            }
        }

        public static string GetText(this OfferAvailability availability) {
            switch (availability) {
                case OfferAvailability.All:
                    return "pro Všechny";
                case OfferAvailability.Alive:
                    return "pro Živé";
                case OfferAvailability.Humans:
                    return "pro Lidi";
                case OfferAvailability.SCPs:
                    return "pro SCP";
                case OfferAvailability.Spectators:
                    return "pro Diváky";
                case OfferAvailability.Private:
                    return "Privátní";
                default: return "&lt;REDACTED&gt;";
            }
        }
    }
}
