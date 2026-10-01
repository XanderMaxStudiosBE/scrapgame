using UnityEngine;
namespace Scrapshift
{
    public static class YardItemVisual
    {
        public static GameObject Create(MaterialKind kind, Transform parent)
        {
            if (kind == MaterialKind.Wire || kind == MaterialKind.Copper) return YardGeometry.Bundle(kind, parent);
            if(kind==MaterialKind.BrokenRadio || kind==MaterialKind.RestoredRadio)return Radio(kind,parent);
            var root = new GameObject(kind == MaterialKind.RestoredFan ? "Tested desk fan" : "Broken desk fan");
            root.transform.SetParent(parent, false);
            if (!AuthoredYardProps.TryPlace("SalvageFan", root.transform, Vector3.zero, out GameObject model))
            {
                YardGeometry.SurfaceBox("Fan base", root.transform, new Vector3(0, .045f, 0), new Vector3(.44f, .09f, .3f), RetroSurface.DarkMetal, false);
                YardProps.Cylinder("Fan stand", root.transform, new Vector3(0,.3f,0), .055f,.47f,RetroSurface.DarkMetal,Quaternion.identity);
                YardProps.Cylinder("Fan guard", root.transform, new Vector3(0,.65f,0), .26f,.1f,RetroSurface.CorrugatedMetal,Quaternion.Euler(90,0,0));
            }
            if (kind == MaterialKind.RestoredFan)
                CozyYardDetails.Accent(root.transform, "Tested label", new Vector3(0,.10f,-.16f), new Vector3(.15f,.055f,.02f), CozyYardDetails.Sage);
            var collider = root.AddComponent<BoxCollider>(); collider.center = new Vector3(0,.46f,0); collider.size = new Vector3(.60f,.95f,.40f);
            return root;
        }
        static GameObject Radio(MaterialKind kind,Transform parent)
        {
            var root=new GameObject(kind==MaterialKind.RestoredRadio?"Tested portable radio":"Broken portable radio");
            root.transform.SetParent(parent,false);
            if(!AuthoredYardProps.TryPlace("PortableRadio",root.transform,Vector3.zero,out GameObject model))
            {
                YardGeometry.SurfaceBox("Radio housing",root.transform,new Vector3(0,.18f,0),new Vector3(.62f,.32f,.22f),RetroSurface.DarkMetal,false);
                CozyYardDetails.Accent(root.transform,"Radio tuning scale",new Vector3(.13f,.23f,-.12f),new Vector3(.22f,.10f,.025f),CozyYardDetails.WarmWindow);
            }
            if(kind==MaterialKind.RestoredRadio)CozyYardDetails.Accent(root.transform,"Tested radio label",new Vector3(.17f,.06f,-.125f),new Vector3(.13f,.04f,.015f),CozyYardDetails.Sage);
            var collider=root.AddComponent<BoxCollider>();collider.center=new Vector3(0,.30f,0);collider.size=new Vector3(.68f,.61f,.28f);
            return root;
        }
        public static string Label(MaterialKind kind)
        {
            switch (kind)
            {
                case MaterialKind.BrokenFan: return "broken desk fan";
                case MaterialKind.RestoredFan: return "tested desk fan";
                case MaterialKind.BrokenRadio:return "broken portable radio";
                case MaterialKind.RestoredRadio:return "tested portable radio";
                case MaterialKind.Copper: return "copper";
                default: return "scrap wire";
            }
        }
    }
}
