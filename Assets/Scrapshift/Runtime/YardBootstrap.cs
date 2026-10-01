using UnityEngine;

namespace Scrapshift
{
    public sealed class YardBootstrap : MonoBehaviour
    {
        public PrototypeBalance balance;
        public Texture2D logo;
        public Shader surfaceShader;
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
            YardProps.Delivery(root, new Vector3(-7, 0, 2));
            var bench = YardProps.Workbench(root, new Vector3(-2.5f, 0, 4));
            YardProps.Buyer(root, new Vector3(2.2f, 0, 4));
            var stripper = WireStripperVisual.Build(root, new Vector3(7, 0, 2));
            YardGeometry.Sign(root, "1 / DELIVERY\nSCRAP WIRE", new Vector3(-7, 2.25f, 2));
            YardGeometry.Sign(root, "2 / WORKBENCH\nMANUAL STRIPPING", new Vector3(-2.5f, 2.35f, 4));
            YardGeometry.Sign(root, "3 / COPPER BUYER\nSELL MATERIAL", new Vector3(2.2f, 2.35f, 4));
            YardGeometry.Sign(root, "4 / POWERED STRIPPER\nFEED WIRE AT THE FRONT", new Vector3(7, 2.35f, 2));

            var playerObject = new GameObject("Player"); playerObject.transform.SetParent(root, false); playerObject.layer = 2;
            var controller = playerObject.AddComponent<CharacterController>(); controller.height = 1.8f; controller.radius = .3f; controller.stepOffset = .25f;
            var player = playerObject.AddComponent<FirstPersonController>();
            var cameraObject = new GameObject("Player Camera"); cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(playerObject.transform, false); cameraObject.transform.localPosition = new Vector3(0, .65f, 0);
            var camera = cameraObject.AddComponent<Camera>(); camera.nearClipPlane = .05f; camera.farClipPlane = 160; camera.fieldOfView = 72;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = ScrapyardWorld.SkyColor;
            cameraObject.AddComponent<AudioListener>(); player.view = camera;
            var sun = new GameObject("Late afternoon sun").AddComponent<Light>(); sun.transform.SetParent(root, false);
            sun.type = LightType.Directional; sun.intensity = 1.6f; sun.color = new Color(1, .85f, .65f); sun.transform.rotation = Quaternion.Euler(45, -35, 0); sun.shadows = LightShadows.Soft;
            var lamp = new GameObject("Warm workshop lamp").AddComponent<Light>(); lamp.transform.SetParent(root, false);
            lamp.transform.localPosition = new Vector3(-2.5f, 2.8f, 3.5f); lamp.type = LightType.Point; lamp.range = 6; lamp.intensity = 3; lamp.color = new Color(1, .65f, .35f);
            // A cool sky fill and warm ground bounce keep shadowed scrap readable.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.60f, .65f, .67f);
            RenderSettings.ambientEquatorColor = new Color(.47f, .46f, .39f);
            RenderSettings.ambientGroundColor = new Color(.32f, .29f, .23f);
            RenderSettings.fog = true; RenderSettings.fogColor = ScrapyardWorld.SkyColor; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 45; RenderSettings.fogEndDistance = 135;

            var game = root.gameObject.AddComponent<PrototypeGame>(); game.balance = balance; game.player = player; game.logo = logo;
            game.benchDisplay = bench.materialDisplay;
            game.machineDisplay = stripper.outputDisplay;
            game.rotor = stripper.rotor; game.additionalRoller = stripper.additionalRoller;
            game.feedDisplay = stripper.feedDisplay; game.machineLamp = stripper.statusLamp;
        }
    }
}
