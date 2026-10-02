using UnityEngine;

namespace Scrapshift.Compact
{
    public sealed class CompactYardBootstrap : MonoBehaviour
    {
        public CompactBalance balance;
        public PrototypeBalance legacyBalance;
        public Texture2D logo;
        public Shader surfaceShader;
        public YardLightingProfile lightingProfile;
        void Awake()
        {
            YardGeometry.SurfaceShader=surfaceShader;
            var root=new GameObject("Compact Scrapshift yard").transform;root.SetParent(transform,false);
            var environment=new GameObject("Fixed infrastructure");environment.transform.SetParent(root,false);
            var handles=CompactYardWorld.Build(environment.transform);
            Attach(handles.Shop,CompactTargetKind.Shop);Attach(handles.Sales,CompactTargetKind.Sales);
            Attach(handles.Delivery,CompactTargetKind.Delivery);Attach(handles.Wire,CompactTargetKind.Wire);
            var playerObject=new GameObject("Player");playerObject.transform.SetParent(root,false);playerObject.layer=2;
            var controller=playerObject.AddComponent<CharacterController>();controller.height=1.8f;controller.radius=.3f;controller.stepOffset=.25f;
            var player=playerObject.AddComponent<FirstPersonController>();
            var cameraObject=new GameObject("Player camera");cameraObject.tag="MainCamera";cameraObject.transform.SetParent(playerObject.transform,false);
            cameraObject.transform.localPosition=new Vector3(0,.65f,0);
            var camera=cameraObject.AddComponent<Camera>();camera.nearClipPlane=.05f;camera.farClipPlane=110;camera.fieldOfView=72;
            cameraObject.AddComponent<AudioListener>();player.view=camera;
            var game=root.gameObject.AddComponent<CompactYardGame>();game.player=player;
            game.balance=balance!=null?balance:Resources.Load<CompactBalance>("ScrapshiftCompact/Balance");game.legacyBalance=legacyBalance;game.logo=logo;
            var profile=lightingProfile!=null?lightingProfile:Resources.Load<YardLightingProfile>("ScrapshiftLighting/CozyAfternoon");
            if(profile!=null)game.lighting=YardLighting.Build(root,camera,profile,new[]{
                CompactYardWorld.ShopAnchor+new Vector3(0,2.65f,-.35f),
                CompactYardWorld.SalesAnchor+new Vector3(0,2.65f,-.35f),CompactYardWorld.DeliveryLightAnchor});
            else Debug.LogError("Missing CozyAfternoon lighting resource.");
        }
        static void Attach(GameObject owner,CompactTargetKind kind)
        {var target=owner.AddComponent<CompactInteractionTarget>();target.kind=kind;}
    }
}
