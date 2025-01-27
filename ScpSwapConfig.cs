using helpers.Configuration;

namespace SCPSwap {
    public static class ScpSwapConfig {

        [Config(Name = "CreateSwapTimeout", Description = "After this time, creating new Swap Requests will be locked (in seconds) (-1 for infinite).")]
        public static int CreateSwapTimeout { get; set; } = -1;


        [Config(Name = "SwapRequestTimeout", Description = "When this time passes, individual Swap Requests will expire, if noone accept offer (in seconds).")]
        public static float SwapRequestTimeout { get; set; } = 25;


        [Config(Name = "AfkSwapTime", Description = "After this time, all AFK SCPs automatically offer it's role for Spectators (in seconds);")]
        public static float AfkSwapTime { get; set; } = 90;


        [Config(Name = "AfkSwapWarningTime", Description = "After this time, all AFK SCPs will be Warned with message in 'AfkSwapWarningMessage' (in seconds);")]
        public static float AfkSwapWarningTime { get; set; } = 75;

        [Config(Name = "AfkSwapWarningMessage", Description = "AFK Warning message to be shown")]
        public static string AfkSwapWarningMessage { get; set; } = "<color=red><b>AFK Varování:</b>\n za 15 vteřin nabídneme tvé SCP někomu jinému,\n pokud se nepohneš!</color>";


        [Config(Name = "RouletteEnabled", Description = "True = player will be chosen from all participants; False = first player enrolled will be chosen.")]
        public static bool RouletteEnabled { get; set; } = true;


        [Config(Name = "RouletteThreshold", Description = "Specifies how many players have to be enrolled to start Roulette. Roulette starts after SwapRequestTimeout otherwise.")]
        public static int RouletteThreshold { get; set; } = 3;


        [Config(Name = "PreserveHealth", Description = "True = Players will also swap HP value; False = Both Players will have max HP.")]
        public static bool PreserveHealth { get; set; } = true;


        [Config(Name = "MaxPlayerSwaps", Description = "Max Swaps that can Player make per round.")]
        public static int MaxPlayerSwaps { get; set; } = 2;

    }
}
