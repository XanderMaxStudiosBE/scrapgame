using System;
using System.Collections.Generic;
using UnityEngine;
namespace Scrapshift
{
    // Cached private skin. No shared editor/game GUI skin is modified.
    public sealed class YardInterfaceTheme : IDisposable
    {
        GUISkin skin;
        readonly List<Texture2D> textures=new List<Texture2D>();
        public Scope Begin(float scale)
        {
            if(skin==null)Build();
            var scope=new Scope(GUI.skin,GUI.matrix,GUI.color,GUI.backgroundColor,GUI.contentColor);
            GUI.skin=skin;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            GUI.color=Color.white;GUI.backgroundColor=Color.white;GUI.contentColor=Color.white;
            return scope;
        }
        void Build()
        {
            skin=UnityEngine.Object.Instantiate(GUI.skin);skin.name="Scrapshift private interface skin";
            Color ink=new Color(.91f,.89f,.80f), muted=new Color(.67f,.69f,.65f);
            skin.label.fontSize=17;skin.label.normal.textColor=ink;skin.label.padding=new RectOffset(1,1,3,3);
            skin.box.fontSize=17;skin.box.normal.textColor=ink;skin.box.normal.background=Panel(new Color(.12f,.15f,.14f,.94f),new Color(.37f,.40f,.35f,.95f));
            skin.box.border=new RectOffset(6,6,6,6);skin.box.padding=new RectOffset(14,14,10,10);
            skin.button.fontSize=17;skin.button.border=new RectOffset(6,6,6,6);skin.button.padding=new RectOffset(12,12,8,8);
            skin.button.normal.background=Panel(new Color(.23f,.28f,.25f),new Color(.40f,.44f,.37f));skin.button.normal.textColor=ink;
            skin.button.hover.background=Panel(new Color(.36f,.40f,.31f),new Color(.69f,.62f,.41f));skin.button.hover.textColor=Color.white;
            skin.button.active.background=Panel(new Color(.61f,.48f,.29f),new Color(.85f,.72f,.46f));skin.button.active.textColor=Color.white;
            skin.button.focused=skin.button.hover;
            skin.button.onNormal=skin.button.active;skin.button.onHover=skin.button.hover;
            skin.button.onActive=skin.button.active;skin.button.onFocused=skin.button.active;
            skin.toggle.fontSize=17;skin.toggle.normal.textColor=ink;skin.toggle.onNormal.textColor=ink;
            skin.window.normal.textColor=muted;
        }
        Texture2D Panel(Color fill,Color edge)
        {
            const int n=20;var texture=new Texture2D(n,n,TextureFormat.RGBA32,false);texture.name="Scrapshift UI panel";texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;
            var pixels=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                bool corner=(x<3||x>=n-3)&&(y<3||y>=n-3);
                pixels[y*n+x]=corner?Color.clear:(x==0||y==0||x==n-1||y==n-1?edge:fill);
            }
            texture.SetPixels(pixels);texture.Apply(false,true);textures.Add(texture);return texture;
        }
        public void Dispose(){if(skin!=null)UnityEngine.Object.Destroy(skin);foreach(var texture in textures)UnityEngine.Object.Destroy(texture);textures.Clear();}
        public struct Scope : IDisposable
        {
            readonly GUISkin skin;readonly Matrix4x4 matrix;readonly Color color,background,content;
            public Scope(GUISkin skin,Matrix4x4 matrix,Color color,Color background,Color content){this.skin=skin;this.matrix=matrix;this.color=color;this.background=background;this.content=content;}
            public void Dispose(){GUI.skin=skin;GUI.matrix=matrix;GUI.color=color;GUI.backgroundColor=background;GUI.contentColor=content;}
        }
    }
}
