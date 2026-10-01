using UnityEngine;
namespace Scrapshift
{
    // A fixed set of decorative meshes. No simulation, colliders or per-frame spawning.
    public sealed class WorkbenchJobVisual
    {
        public readonly Transform wire, copper, tool;
        readonly Transform leftJaw, rightJaw;
        float pulseAge = Duration;
        const float Duration = .22f;
        public WorkbenchJobVisual(Transform anchor)
        {
            // Preserve the bootstrap anchor and its mesh for compatibility, but replace its block rendering.
            anchor.GetComponent<Renderer>().enabled = false;
            anchor.localScale = Vector3.one;
            wire = YardGeometry.Bundle(MaterialKind.Wire, anchor).transform;
            copper = YardGeometry.Bundle(MaterialKind.Copper, anchor).transform;
            foreach (var collider in anchor.GetComponentsInChildren<Collider>()) collider.enabled = false;
            wire.localPosition = copper.localPosition = new Vector3(0,-.06f,0);
            tool = new GameObject("Working stripping pliers").transform;
            tool.SetParent(anchor,false);
            leftJaw = Jaw(tool, -.045f); rightJaw = Jaw(tool, .045f);
            ApplyPose(0);
        }
        static Transform Jaw(Transform parent, float x)
        {
            var jaw = new GameObject("Pliers half").transform;
            jaw.SetParent(parent,false); jaw.localPosition = new Vector3(x,0,0);
            YardGeometry.SurfaceBox("Worn grip",jaw,new Vector3(0,0,-.13f),new Vector3(.04f,.04f,.23f),RetroSurface.RustPaint,false);
            YardGeometry.SurfaceBox("Steel cutting jaw",jaw,new Vector3(0,0,.07f),new Vector3(.04f,.045f,.13f),RetroSurface.DarkMetal,false);
            return jaw;
        }
        public void Refresh(YardModel model)
        {
            wire.gameObject.SetActive(model.State.benchLoaded);
            bool processing = model.State.benchLoaded;
            float progress = processing ? (float)model.State.benchStrokes/model.WireWorkSteps : 1;
            copper.gameObject.SetActive(model.State.benchOutput > 0 || (processing && model.State.benchStrokes > 0));
            wire.localScale = Vector3.one * (1-.3f*progress);
            copper.localScale = Vector3.one * (processing ? .5f+.5f*progress : 1);
            copper.localPosition = processing ? new Vector3(.32f,-.09f,.14f) : new Vector3(0,-.09f,0);
            tool.gameObject.SetActive(model.State.benchLoaded);
        }
        public void Pulse() { pulseAge = 0; }
        public void Step(float seconds)
        {
            if (pulseAge >= Duration) return;
            pulseAge = Mathf.Min(Duration,pulseAge + Mathf.Max(0,seconds));
            float stroke = Mathf.Sin(pulseAge / Duration * Mathf.PI);
            if (pulseAge >= Duration) stroke = 0;
            ApplyPose(stroke);
        }
        void ApplyPose(float stroke)
        {
            tool.localPosition = Vector3.Lerp(new Vector3(.46f,-.105f,-.23f),new Vector3(.04f,.08f,-.09f),stroke);
            tool.localRotation = Quaternion.Euler(0,-25+stroke*25,stroke*-12);
            leftJaw.localRotation = Quaternion.Euler(0,8*(1-stroke),0);
            rightJaw.localRotation = Quaternion.Euler(0,-8*(1-stroke),0);
        }
    }
}
