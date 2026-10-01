using System;
using UnityEngine;
namespace Scrapshift
{
    public enum YardSound { Pickup, Tool, Sale }
    // Four bounded sources. No per-frame object searches or clip synthesis.
    public sealed class YardAudio : IDisposable
    {
        readonly GameObject root;
        readonly AudioSource effects, machine, fan, ambience;
        readonly AudioClip pickup, stroke, sale, step;
        readonly FirstPersonController player;
        readonly PresentationSettings settings;
        Vector3 previousPosition;
        float stepDistance;
        bool loopsPaused;
        public YardAudio(Transform parent, FirstPersonController player, PresentationSettings settings)
        {
            this.player=player; this.settings=settings; previousPosition=player.transform.position;
            root=new GameObject("Yard soundscape"); root.transform.SetParent(parent,false);
            effects=Source("Tools and footsteps",Vector3.zero,false);
            machine=Source("Stripper motor",new Vector3(7,1,2),true);
            fan=Source("Tested fan",FanWorkbenchVisual.Position+Vector3.up*1.5f,true);
            ambience=Source("Outdoor breeze",Vector3.zero,false);
            pickup=Clip("Pickup");stroke=Clip("ToolStroke");sale=Clip("Sale");step=Clip("GravelStep");
            machine.clip=Clip("StripperLoop"); fan.clip=Clip("FanLoop");ambience.clip=Clip("YardAmbience");
            machine.loop=fan.loop=ambience.loop=true;
            ApplyVolumes();if(ambience.clip!=null)ambience.Play();
        }
        static AudioClip Clip(string name)
        {
            var clip=Resources.Load<AudioClip>("ScrapshiftAudio/"+name);
            if(clip==null)Debug.LogWarning("Missing yard sound: "+name);
            return clip;
        }
        AudioSource Source(string name,Vector3 position,bool spatial)
        {
            var go=new GameObject(name);go.transform.SetParent(root.transform,false);go.transform.localPosition=position;
            var source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=spatial?1:0;
            source.minDistance=1.5f;source.maxDistance=14;source.rolloffMode=AudioRolloffMode.Linear;source.dopplerLevel=0;
            return source;
        }
        public void ApplyVolumes()
        {
            var p=settings.Preferences;effects.volume=p.masterVolume*p.effectsVolume;
            machine.volume=p.masterVolume*p.effectsVolume*.65f;fan.volume=p.masterVolume*p.effectsVolume*.45f;
            ambience.volume=p.masterVolume*p.ambienceVolume;
        }
        public void Play(YardSound sound)
        {
            AudioClip clip=sound==YardSound.Tool?stroke:sound==YardSound.Sale?sale:pickup;
            effects.pitch=1;if(clip!=null)effects.PlayOneShot(clip);
        }
        public void Pause(bool paused)
        {
            if(paused==loopsPaused)return;loopsPaused=paused;
            if(paused){effects.Stop();machine.Pause();fan.Pause();}
            else {machine.UnPause();fan.UnPause();previousPosition=player.transform.position;stepDistance=0;}
        }
        public void Step(YardModel model)
        {
            Loop(machine,model.State.machineRemaining>0);Loop(fan,model.State.fanStage==FanStage.Tested);
            Vector3 pos=player.transform.position,delta=pos-previousPosition;previousPosition=pos;delta.y=0;
            float distance=delta.magnitude;
            if(!player.Grounded || distance>2){stepDistance=0;return;}
            stepDistance+=distance;
            if(stepDistance>=1.65f)
            {
                stepDistance=0;effects.pitch=UnityEngine.Random.Range(.94f,1.06f);
                if(step!=null)effects.PlayOneShot(step,.65f);
            }
        }
        static void Loop(AudioSource source,bool active)
        {
            if(active && !source.isPlaying && source.clip!=null)source.Play();
            else if(!active && source.isPlaying)source.Stop();
        }
        public void Dispose(){if(root!=null)UnityEngine.Object.Destroy(root);}
    }
}
