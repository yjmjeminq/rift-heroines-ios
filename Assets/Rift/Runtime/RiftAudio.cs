using System;
using UnityEngine;

namespace RiftHeroines
{
    // Original synthesized score and SFX; no third-party audio or network calls.
    public sealed class RiftAudio : MonoBehaviour
    {
        AudioSource[] voices;
        AudioSource music;
        AudioClip hit, bladeHit,magicHit,slash,summon,heal,ultimate,chime,shield,perfect,warning,ready;
        readonly float[] voiceEnds=new float[10];readonly int[] voicePriorities=new int[10];
        readonly float[] voiceVolumes=new float[10];
        float lastHit=-1,lastHeal=-1,duckUntil;
        public bool Muted;
        public int PlayedHits {get;private set;}
        void Awake()
        {
            voices=new AudioSource[10];for(int i=0;i<voices.Length;i++){voices[i]=gameObject.AddComponent<AudioSource>();voices[i].playOnAwake=false;voices[i].volume=.32f;}
            music=gameObject.AddComponent<AudioSource>();music.loop=true;music.volume=.085f;music.playOnAwake=false;
            hit=PreviewClip(0);slash=PreviewClip(1);summon=PreviewClip(2);heal=PreviewClip(3);
            ultimate=PreviewClip(4);chime=PreviewClip(5);shield=PreviewClip(6);bladeHit=PreviewClip(7);magicHit=PreviewClip(8);perfect=PreviewClip(9);warning=PreviewClip(10);ready=PreviewClip(11);
            music.clip=Score();music.Play();
        }
        public void Play(Cue cue,float x,bool critical=false,int hero=-1)
        {
            if(Muted)return;
            AudioClip clip=hit;int priority=1;
            switch(cue)
            {
                case Cue.Hit:case Cue.BaseHit:if(Time.unscaledTime-lastHit<.055f)return;lastHit=Time.unscaledTime;
                    int style=hero>=0&&hero<RiftData.HeroCount?RiftFeedback.HitStyle(RiftData.Heroes[hero].power):3;
                    clip=style==4?bladeHit:style==5?magicHit:hit;priority=2;break;
                case Cue.Windup: return;
                case Cue.Spawn:clip=summon;break;
                case Cue.Heal:if(Time.unscaledTime-lastHeal<.25f)return;lastHeal=Time.unscaledTime;clip=heal;priority=0;break;
                case Cue.Ultimate:case Cue.Boss:clip=ultimate;priority=3;duckUntil=Time.unscaledTime+1.3f;break;
                case Cue.StageClear:case Cue.Bond:case Cue.Capture:clip=chime;break;
                case Cue.Shield:clip=shield;priority=2;break;
                case Cue.PerfectGuard:clip=perfect;priority=4;duckUntil=Time.unscaledTime+1;break;
                case Cue.Warning:clip=warning;priority=4;duckUntil=Time.unscaledTime+2.5f;break;
                case Cue.Chain:clip=ultimate;break;
                case Cue.Order:case Cue.Death:clip=slash;break;
                default:return;
            }
            bool played=PlayVoice(clip,x,priority,critical?.84f:UnityEngine.Random.Range(.96f,1.04f),cue==Cue.Ultimate?.38f:cue==Cue.Spawn?.10f:cue==Cue.Heal?.13f:.25f);
            if(played&&(cue==Cue.Hit||cue==Cue.BaseHit))PlayedHits++;
        }
        public void CounterWindow(){if(!Muted)PlayVoice(ready,50,4,1,.23f);}
        bool PlayVoice(AudioClip clip,float x,int priority,float pitch,float volume)
        {
            int selected=RiftVoicePolicy.Select(voiceEnds,voicePriorities,Time.unscaledTime,priority);if(selected<0)return false;
            voiceEnds[selected]=Time.unscaledTime+clip.length/pitch;voicePriorities[selected]=priority;
            voiceVolumes[selected]=volume;UpdateMix();
            var source=voices[selected];source.clip=clip;source.panStereo=Mathf.Clamp((x-50)/90,-.5f,.5f);source.pitch=pitch;source.Play();return true;
        }
        void UpdateMix(){float sum=0;for(int i=0;i<voices.Length;i++)if(voiceEnds[i]>Time.unscaledTime)sum+=voiceVolumes[i];float gain=RiftVoicePolicy.MixGain(sum);for(int i=0;i<voices.Length;i++)voices[i].volume=voiceVolumes[i]*gain;}
        void Update(){UpdateMix();if(music){music.mute=Muted;music.volume=Mathf.MoveTowards(music.volume,Time.unscaledTime<duckUntil?.027f:.085f,Time.unscaledDeltaTime*.2f);}foreach(var source in voices)source.mute=Muted;}
        public static AudioClip PreviewClip(int kind)
        {
            string[] names={"Impact","Slash","Summon","Heal","Rift break","Clear","Ward","Blade impact","Arcane impact","Perfect guard","Boss warning","Counter ready"};
            float[] lengths={.19f,.15f,.45f,.5f,1.1f,.85f,.55f,.18f,.24f,.55f,.7f,.13f};
            if(kind<0||kind>=names.Length)throw new ArgumentOutOfRangeException("kind");return Make(names[kind],lengths[kind],kind);
        }
        static AudioClip Make(string name,float seconds,int kind)
        {
            int rate=22050,n=(int)(seconds*rate);var samples=new float[n];var rng=new System.Random(340+kind);float filtered=0;
            for(int i=0;i<n;i++)
            {
                float t=i/(float)rate,p=t/seconds,noise=(float)rng.NextDouble()*2-1;filtered=filtered*.65f+noise*.35f;
                float env=Mathf.Pow(1-p,2)*Mathf.Min(1,t*250),s=0;
                if(kind==0)s=(Mathf.Sin(2*Mathf.PI*(95*t-90*t*t))*.7f+noise*.45f)*Mathf.Exp(-t*20)*Mathf.Min(1,t*1200);
                else if(kind==1)s=filtered*env*Mathf.Sin(Mathf.PI*p)*.8f;
                else if(kind==2)s=(Mathf.Sin(2*Mathf.PI*(450*t+300*t*t))+.35f*Mathf.Sin(2*Mathf.PI*900*t))*env*.35f;
                else if(kind==3)s=(Mathf.Sin(2*Mathf.PI*660*t)+Mathf.Sin(2*Mathf.PI*880*t)*.5f)*env*.3f;
                else if(kind==4)s=(Mathf.Sin(2*Mathf.PI*(70*t-18*t*t))*.8f+filtered*.65f)*env+Mathf.Sin(2*Mathf.PI*330*t)*Mathf.Exp(-t*8)*.2f;
                else if(kind==5){float freq=t<.16f?523:t<.32f?659:784;s=Mathf.Sin(2*Mathf.PI*freq*t)*env*.5f;}
                else if(kind==6)s=(Mathf.Sin(2*Mathf.PI*330*t)+Mathf.Sin(2*Mathf.PI*495*t)*.5f)*env*.35f;
                else if(kind==7)s=((noise-filtered)*.65f+Mathf.Sin(2*Mathf.PI*(1450*t-1100*t*t))*.34f)*env*Mathf.Exp(-t*10);
                else if(kind==8)s=(Mathf.Sin(2*Mathf.PI*(470*t+900*t*t))*.48f+filtered*.25f)*env*Mathf.Exp(-t*5);
                else if(kind==9)s=(Mathf.Sin(2*Mathf.PI*523*t)+.6f*Mathf.Sin(2*Mathf.PI*784*t)+.35f*Mathf.Sin(2*Mathf.PI*1047*t))*env*.38f;
                else if(kind==10){float pulse=t%.23f;s=Mathf.Sin(2*Mathf.PI*(720*t+200*t*t))*.5f*Mathf.Exp(-pulse*22)*env;}
                else s=(Mathf.Sin(2*Mathf.PI*1320*t)+.4f*Mathf.Sin(2*Mathf.PI*1760*t))*env*.32f;
                if(kind==0)s*=.75f;else if(kind==4||kind==7)s*=.5f;
                samples[i]=Mathf.Clamp(s,-.9f,.9f);
            }
            var clip=AudioClip.Create(name,n,1,rate,false);clip.SetData(samples,0);return clip;
        }
        static AudioClip Score()
        {
            const int rate=22050,seconds=16;var data=new float[rate*seconds];int[] notes={62,69,74,77,69,65,72,74,62,69,74,81,77,74,72,69};
            for(int i=0;i<data.Length;i++)
            {
                float t=i/(float)rate;int beat=(int)(t*2);float local=t-(beat*.5f),f=440*Mathf.Pow(2,(notes[(beat/2)%notes.Length]-69)/12f);
                float bell=(Mathf.Sin(2*Mathf.PI*f*t)+.22f*Mathf.Sin(2*Mathf.PI*f*2.003f*t))*Mathf.Exp(-local*5);
                float bass=Mathf.Sin(2*Mathf.PI*(beat/8%2==0?73.416f:65.406f)*t)*.22f;
                float fade=Mathf.Min(1,Mathf.Min(t,.999f*seconds-t)*2);
                data[i]=(bell*.25f+bass)*Mathf.Max(0,fade);
            }
            var clip=AudioClip.Create("Moonbridge original ambient score",data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
    }
    public static class RiftVoicePolicy
    {
        public static float MixGain(float sum){return sum<=.75f?1:.75f/sum;}
        public static int Select(float[] ends,int[] priorities,float now,int incoming)
        {
            int chosen=-1;for(int i=0;i<ends.Length;i++)
            {if(ends[i]<=now)return i;if(priorities[i]<=incoming&&(chosen<0||priorities[i]<priorities[chosen]||priorities[i]==priorities[chosen]&&ends[i]<ends[chosen]))chosen=i;}
            return chosen;
        }
    }
}
