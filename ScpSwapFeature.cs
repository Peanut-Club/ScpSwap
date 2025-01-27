using Compendium.Features;

namespace SCPSwap {
    public class ScpSwapFeature : ConfigFeatureBase {
        public override string Name => "SCPSwap";

        public override bool IsPatch => false;

        public override bool CanBeShared => true;


    }
}
