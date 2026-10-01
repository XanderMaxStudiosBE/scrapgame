using UnityEngine;
namespace Scrapshift
{
    public sealed partial class PrototypeGame
    {
        GUIStyle hudTitleStyle,hudSmallStyle,hudContextStyle;
        void DrawGameplayHud(float width,float height)
        {
            if(hudTitleStyle==null)
            {
                hudTitleStyle=new GUIStyle(GUI.skin.label){fontSize=18,fontStyle=FontStyle.Bold};
                hudSmallStyle=new GUIStyle(controlLegend){fontSize=13};
                hudContextStyle=new GUIStyle(wrappedLabel){fontSize=16};
            }
            float left=22,objectiveWidth=Mathf.Min(width-44,350);
            GUI.Box(new Rect(left,18,objectiveWidth,43),"");
            GUI.Label(new Rect(left+12,24,objectiveWidth-24,30),hudDay,hudTitleStyle);
            float textHeight=hudSmallStyle.CalcHeight(new GUIContent(hudObjective),objectiveWidth-24);
            float objectiveHeight=Mathf.Max(42,textHeight+18);
            GUI.Box(new Rect(left,69,objectiveWidth,objectiveHeight),"");
            GUI.Label(new Rect(left+12,77,objectiveWidth-24,textHeight+3),hudObjective,hudSmallStyle);
            float routeY=77+objectiveHeight;
            GUI.Box(new Rect(left,routeY,objectiveWidth,53),"");
            GUI.Label(new Rect(left+12,routeY+5,objectiveWidth-24,24),hudRoute,hudSmallStyle);
            GUI.Label(new Rect(left+12,routeY+27,objectiveWidth-24,22),hudArea,hudSmallStyle);
            if(hudOrder.Length>0 && width>1050)
            {
                float orderHeight=hudSmallStyle.CalcHeight(new GUIContent(hudOrder),236)+44;
                GUI.Box(new Rect(width-282,18,260,orderHeight),"REQUESTS");
                GUI.Label(new Rect(width-270,52,236,orderHeight-38),hudOrder,hudSmallStyle);
            }
            float promptWidth=Mathf.Min(width-44,540),promptTop=height-38;
            bool carrying=Model.Carried!=null;
            if(target!=null || carrying)
            {
                float hintHeight=target!=null?hudContextStyle.CalcHeight(new GUIContent(hudHint.text),promptWidth-28):0;
                float promptHeight=20+hintHeight+(carrying?28:0);
                promptTop=height-48-promptHeight;
                float x=(width-promptWidth)/2,y=promptTop+9;
                GUI.Box(new Rect(x,promptTop,promptWidth,promptHeight),"");
                if(carrying){GUI.Label(new Rect(x+14,y,promptWidth-28,25),hudHeld,hudSmallStyle);y+=28;}
                if(target!=null)GUI.Label(new Rect(x+14,y,promptWidth-28,hintHeight+4),hudHint.text,hudContextStyle);
            }
            if(target!=null && hudProgress.Visible)
            {
                float progressWidth=Mathf.Min(promptWidth,420),x=(width-progressWidth)/2,y=promptTop-64;
                GUI.Box(new Rect(x,y,progressWidth,54),"");
                GUI.Label(new Rect(x+12,y+5,progressWidth-24,24),hudProgress.label,hudSmallStyle);
                FillHud(new Rect(x+12,y+36,progressWidth-24,6),new Color(.23f,.25f,.22f));
                FillHud(new Rect(x+12,y+36,(progressWidth-24)*hudProgress.fraction,6),new Color(.78f,.65f,.40f));
            }
            GUI.Label(new Rect(left,height-30,390,24),"Esc / Menu, map & journal",hudSmallStyle);
            if(presentation.Preferences.showFrameRate)
                GUI.Label(new Rect(width-237,height-30,215,24),frameReadout,hudSmallStyle);
            FillHud(new Rect(width/2-1.5f,height/2-1.5f,3,3),target!=null&&hudHint.canUse?new Color(.77f,.86f,.62f):YardGeometry.Ivory);
            if(Time.unscaledTime<messageUntil && !string.IsNullOrEmpty(message))
            {
                float w=Mathf.Min(width-44,550),h=hudSmallStyle.CalcHeight(new GUIContent(message),w-28)+20;
                float y=Mathf.Max(routeY+64,height*.27f);
                GUI.Box(new Rect((width-w)/2,y,w,h),"");
                GUI.Label(new Rect((width-w)/2+14,y+8,w-28,h-14),message,hudSmallStyle);
            }
        }
        static void FillHud(Rect area,Color color)
        {GUI.color=color;GUI.DrawTexture(area,Texture2D.whiteTexture);GUI.color=Color.white;}
    }
}
