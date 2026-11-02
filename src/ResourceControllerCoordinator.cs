using BepInEx.Configuration;

namespace GreenHellAutoStart
{
    internal static class ResourceControllerCoordinator
    {
        internal static void Initialize(ConfigFile config)
        {
            PerformanceController.Initialize(config);
            AudioController.Initialize(config);
            CameraController.Initialize(config);
        }

        internal static void Update()
        {
            PerformanceController.Update();
            AudioController.Update();
            CameraController.Update();
        }

        internal static void Restore()
        {
            CameraController.Restore();
            AudioController.Restore();
            PerformanceController.Restore();
        }
    }
}