using System;
using UnityEngine;
namespace Scrapshift
{
    [CreateAssetMenu(menuName="Scrapshift/Yard lighting profile")]
    public sealed class YardLightingProfile : ScriptableObject
    {
        public Color zenith=new Color(.22f,.38f,.52f),horizon=new Color(.76f,.74f,.66f),ground=new Color(.27f,.29f,.25f);
        public Color skyFill=new Color(.48f,.54f,.60f),sideFill=new Color(.38f,.39f,.35f),groundFill=new Color(.22f,.23f,.20f);
        public Color sunColor=new Color(1,.93f,.80f),lampColor=new Color(1,.85f,.64f);
        [Range(.1f,3)] public float sunIntensity=1.25f;
        [Range(20,70)] public float sunElevation=38;
        [Range(-180,180)] public float sunAzimuth=-35;
        [Range(.1f,4)] public float lampIntensity=3.4f;
        [Range(40,110)] public float fogStart=75;
        [Range(120,200)] public float fogEnd=160;
        public void Validate()
        {
            foreach(Color color in new[]{zenith,horizon,ground,skyFill,sideFill,groundFill,sunColor,lampColor})
                if(!Finite(color.r)||!Finite(color.g)||!Finite(color.b)||!Finite(color.a)||color.a<0||color.a>1||color.r<0||color.g<0||color.b<0||color.r>1||color.g>1||color.b>1)throw new ArgumentException("Lighting colors must be finite and between zero and one.");
            if(!InRange(sunIntensity,.1f,3)||!InRange(sunElevation,20,70)||!InRange(sunAzimuth,-180,180)||!InRange(lampIntensity,.1f,4)||!InRange(fogStart,40,110)||!InRange(fogEnd,120,200)||fogStart>=fogEnd)throw new ArgumentException("Invalid yard lighting profile.");
        }
        static bool Finite(float value) { return !float.IsNaN(value)&&!float.IsInfinity(value); }
        static bool InRange(float value,float min,float max) { return Finite(value)&&value>=min&&value<=max; }
        public Color SkyColor(Vector3 direction)
        {
            direction.Normalize();float height=direction.y;
            var color=Color.Lerp(horizon,zenith,Mathf.Pow(Mathf.Clamp01(height),.65f));
            return Color.Lerp(color,ground,Mathf.SmoothStep(0,1,Mathf.Clamp01(-height/.45f)));
        }
    }
}
