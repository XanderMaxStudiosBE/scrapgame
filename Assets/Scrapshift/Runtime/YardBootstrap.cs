using UnityEngine;

namespace Scrapshift
{
    public sealed class YardBootstrap : MonoBehaviour
    {
        public PrototypeBalance balance;
        public Texture2D logo;
        public Shader surfaceShader;
        public YardLightingProfile lightingProfile;
        public static Vector3 StationPosition(YardLandmark landmark)
        {
            var destination=YardNavigation.Get(landmark);
            return new Vector3(destination.x,0,destination.z);
        }
        void Awake()
        {
            YardGeometry.SurfaceShader = surfaceShader;
            var root = new GameObject("Scrapshift Yard").transform;
            root.SetParent(transform, false);
            // Only the stationary ground/fence/workshop/clutter enter this batch.
            // Stations, item views, bench progress and rollers remain independently mutable.
            var environment = new GameObject("Stationary environment");
            environment.transform.SetParent(root, false);
            YardProps.Surroundings(environment.transform);
            // The world builder batches each stationary district separately.
            YardProps.Delivery(root, StationPosition(YardLandmark.Delivery));
            var bench = YardProps.Workbench(root, StationPosition(YardLandmark.Bench));
            YardProps.Buyer(root, StationPosition(YardLandmark.Buyer));
            var stripper = WireStripperVisual.Build(root, StationPosition(YardLandmark.Machine));
            YardGeometry.MountedSign(root, "WIRE DELIVERY", StationPosition(YardLandmark.Delivery) + Vector3.forward * .7f);
            YardGeometry.MountedSign(root, "HAND BENCH", StationPosition(YardLandmark.Bench) + Vector3.forward * .7f);
            YardGeometry.MountedSign(root, "SCRAP BUYER", StationPosition(YardLandmark.Buyer) + Vector3.forward * .7f);
            YardGeometry.MountedSign(root, "WIRE STRIPPER", StationPosition(YardLandmark.Machine) + Vector3.forward * .7f);

            var playerObject = new GameObject("Player"); playerObject.transform.SetParent(root, false); playerObject.layer = 2;
            var controller = playerObject.AddComponent<CharacterController>(); controller.height = 1.8f; controller.radius = .3f; controller.stepOffset = .25f;
            var player = playerObject.AddComponent<FirstPersonController>();
            var cameraObject = new GameObject("Player Camera"); cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(playerObject.transform, false); cameraObject.transform.localPosition = new Vector3(0, .65f, 0);
            var camera = cameraObject.AddComponent<Camera>(); camera.nearClipPlane = .05f; camera.farClipPlane = 160; camera.fieldOfView = 72;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = ScrapyardWorld.SkyColor;
            cameraObject.AddComponent<AudioListener>(); player.view = camera;
            var game = root.gameObject.AddComponent<PrototypeGame>(); game.balance = balance; game.player = player; game.logo = logo;
            game.business = YardBusinessVisual.Build(root);
            game.fanWorkbench = FanWorkbenchVisual.Build(root);
            game.benchDisplay = bench.materialDisplay;
            game.machineDisplay = stripper.outputDisplay;
            game.rotor = stripper.rotor; game.additionalRoller = stripper.additionalRoller;
            game.feedDisplay = stripper.feedDisplay; game.machineLamp = stripper.statusLamp;
            var profile=lightingProfile!=null?lightingProfile:Resources.Load<YardLightingProfile>("ScrapshiftLighting/CozyAfternoon");
            if(profile!=null)game.lighting=YardLighting.Build(root,camera,profile);
            else Debug.LogError("Missing CozyAfternoon lighting profile. Reimport tracked lighting resources.");
        }
    }
}
