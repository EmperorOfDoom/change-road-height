using ICities;
using ChangeRoadHeight.Threading;

namespace ChangeRoadHeight.Loading
{
    public class LoadingExtension : LoadingExtensionBase
    {

        public override void OnLevelLoaded(LoadMode mode)
        {
            if (ThreadingExtension.Instance != null)
            {
                ThreadingExtension.Instance.OnLevelLoaded(mode);
            }
        }

        public override void OnLevelUnloading()
        {
            if (ThreadingExtension.Instance != null)
            {
                ThreadingExtension.Instance.OnLevelUnloading();
            }
        }
    }
}
