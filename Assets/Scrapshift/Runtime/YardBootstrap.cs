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
            YardGeometry.Box("Packed earth", root, new Vector3(0, -.25f, 0), new Vector3(24, .5f, 20), YardGeometry.Olive);
            // Solid collision perimeter with chunky slats and rails.
            YardGeometry.Box("North fence", root, new Vector3(0, 1, 9.5f), new Vector3(24, 2, .2f), YardGeometry.Charcoal);
            YardGeometry.Box("South fence", root, new Vector3(0, 1, -9.5f), new Vector3(24, 2, .2f), YardGeometry.Charcoal);
            YardGeometry.Box("East fence", root, new Vector3(11.5f, 1, 0), new Vector3(.2f, 2, 19), YardGeometry.Charcoal);
            YardGeometry.Box("West fence", root, new Vector3(-11.5f, 1, 0), new Vector3(.2f, 2, 19), YardGeometry.Charcoal);
            for (int i = -11; i <= 11; i++)
                foreach (int side in new[] { -1, 1 })
                    YardGeometry.Box("Fence post", root, new Vector3(i, 1.25f, side * 9.4f), new Vector3(.18f, 2.5f, .3f), YardGeometry.Rust);
            // All stations face the starting position, in a readable left-to-right route.
            YardGeometry.Station(root, "Delivery crate", TargetKind.Supply, new Vector3(-7, .6f, 2), YardGeometry.Olive);
            YardGeometry.Sign(root, "1 / DELIVERY\nFree scrap wire", new Vector3(-7, 2.1f, 2));
            YardGeometry.Station(root, "Stripping workbench", TargetKind.Bench, new Vector3(-2.5f, .6f, 4), YardGeometry.Rust);
            YardGeometry.Sign(root, "2 / WORKBENCH\nStrip wire by hand", new Vector3(-2.5f, 2.1f, 4));
            YardGeometry.Station(root, "Copper buyer", TargetKind.Sell, new Vector3(2.2f, .6f, 4), YardGeometry.Olive);
            YardGeometry.Sign(root, "3 / SELL COPPER\nEarn your first machine", new Vector3(2.2f, 2.1f, 4));
            YardGeometry.Station(root, "Powered wire stripper", TargetKind.Machine, new Vector3(7, .6f, 2), YardGeometry.Rust);
            YardGeometry.Sign(root, "4 / POWERED STRIPPER\nBuy / Feed / Collect", new Vector3(7, 2.1f, 2));
            // Corrugated covered workshop. Pillars leave the station fronts accessible.
            foreach (float x in new[] { -4.5f, -.5f })
                YardGeometry.Box("Workshop pillar", root, new Vector3(x, 1.6f, 5.2f), new Vector3(.15f, 3.2f, .15f), YardGeometry.Charcoal);
            for (int i = 0; i < 15; i++)
                YardGeometry.Box("Corrugated roof", root, new Vector3(-4.6f + i * .3f, 3.25f + (i % 2) * .04f, 4.3f), new Vector3(.32f, .09f, 2.8f), YardGeometry.Charcoal);
            YardGeometry.Box("Collection bin", root, new Vector3(4.5f, .4f, 6.2f), new Vector3(1.1f, .8f, 1.1f), YardGeometry.Copper);
            YardGeometry.Sign(root, "COPPER BUYER", new Vector3(4.5f, 1.25f, 6.2f));
            for (int i = 0; i < 7; i++)
                YardGeometry.Box("Salvage pile", root, new Vector3(-9 + (i % 3) * .55f, .2f + (i / 3) * .35f, 7 + (i % 2) * .6f), new Vector3(.7f, .4f, .6f), i % 2 == 0 ? YardGeometry.Rust : YardGeometry.Charcoal);

            var playerObject = new GameObject("Player"); playerObject.transform.SetParent(root, false); playerObject.layer = 2;
            var controller = playerObject.AddComponent<CharacterController>(); controller.height = 1.8f; controller.radius = .3f; controller.stepOffset = .25f;
            var player = playerObject.AddComponent<FirstPersonController>();
            var cameraObject = new GameObject("Player Camera"); cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(playerObject.transform, false); cameraObject.transform.localPosition = new Vector3(0, .65f, 0);
            var camera = cameraObject.AddComponent<Camera>(); camera.nearClipPlane = .05f; camera.farClipPlane = 70; camera.fieldOfView = 72;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = YardGeometry.Charcoal;
            cameraObject.AddComponent<AudioListener>(); player.view = camera;
            var sun = new GameObject("Late afternoon sun").AddComponent<Light>(); sun.transform.SetParent(root, false);
            sun.type = LightType.Directional; sun.intensity = 1.6f; sun.color = new Color(1, .85f, .65f); sun.transform.rotation = Quaternion.Euler(45, -35, 0); sun.shadows = LightShadows.Soft;
            var lamp = new GameObject("Warm workshop lamp").AddComponent<Light>(); lamp.transform.SetParent(root, false);
            lamp.transform.localPosition = new Vector3(-2.5f, 2.8f, 3.5f); lamp.type = LightType.Point; lamp.range = 6; lamp.intensity = 3; lamp.color = new Color(1, .65f, .35f);
            RenderSettings.ambientLight = new Color(.45f, .43f, .38f);
            RenderSettings.fog = true; RenderSettings.fogColor = YardGeometry.Charcoal; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 20; RenderSettings.fogEndDistance = 60;

            var game = root.gameObject.AddComponent<PrototypeGame>(); game.balance = balance; game.player = player; game.logo = logo;
            game.benchDisplay = YardGeometry.Box("Bench material", root, new Vector3(-2.5f, 1.35f, 4), new Vector3(.75f, .2f, .35f), YardGeometry.Copper, false).transform;
            game.machineDisplay = YardGeometry.Box("Output bin copper", root, new Vector3(7.65f, 1.35f, 2), new Vector3(.55f, .2f, .45f), YardGeometry.Copper, false).transform;
            game.rotor = YardGeometry.Box("Exposed drive roller", root, new Vector3(6.35f, 1.4f, 2), new Vector3(.5f, .5f, .25f), YardGeometry.Charcoal, false).transform;
            game.machineLamp = YardGeometry.Box("Machine status lamp", root, new Vector3(7, 1.4f, 2), new Vector3(.2f, .2f, .2f), YardGeometry.Ivory, false).GetComponent<Renderer>();
        }
    }
}
