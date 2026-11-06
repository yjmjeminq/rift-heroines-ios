#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace RiftHeroines.EditorTools
{
    public static partial class RiftValidation
    {
        static void CheckFeedback()
        {
            var ready=TechniqueFixture(0);ready.Tick(ready.WarningTime-.5f);
            Check("A ready perfect counter is offered in its actual timing window",RiftFeedback.CanCounter(ready)&&RiftFeedback.WarningAction(ready)=="지금 E!");
            ready.Summon(5);ready.Summon(4);
            Check("Low mana never displays a false counter instruction",ready.Mana<30&&!RiftFeedback.CanCounter(ready)&&!RiftFeedback.WarningAction(ready).Contains("지금 E")&&RiftFeedback.WarningAction(ready).Contains("부족"));
            var unavailable=TechniqueFixture(0);unavailable.Shield();unavailable.Tick(unavailable.WarningTime-.5f);
            Check("A shield on cooldown offers its real unavailable state",unavailable.ShieldCooldown>0&&!RiftFeedback.WarningAction(unavailable).Contains("지금 E")&&RiftFeedback.WarningAction(unavailable).Contains("재사용"));
            var alternative=TechniqueFixture(0,true);alternative.Shield();alternative.Tick(alternative.WarningTime-.5f);
            Check("A charged ultimate is offered when the counter is unavailable",RiftFeedback.WarningAction(alternative)=="R로 비기 차단 가능");
            Check("Heavy strikes blades and spells use three distinct hit styles",RiftFeedback.HitStyle(HeroPower.ShockPunch)!=RiftFeedback.HitStyle(HeroPower.IceSword)&&RiftFeedback.HitStyle(HeroPower.IceSword)!=RiftFeedback.HitStyle(HeroPower.PaperStorm)&&RiftFeedback.HitStyle(HeroPower.ShockPunch)!=RiftFeedback.HitStyle(HeroPower.PaperStorm));
            float[] ends={10,10,10};int[] priorities={4,3,4};
            Check("Ordinary hits cannot preempt a warning or ultimate voice",RiftVoicePolicy.Select(ends,priorities,1,2)==-1);
            Check("A warning can preempt a lower priority active sound",RiftVoicePolicy.Select(ends,priorities,1,4)==1);
            ends[2]=0;Check("An unused voice is used before interrupting an active sound",RiftVoicePolicy.Select(ends,priorities,1,0)==2);
            Check("Simultaneous effects have a bounded shared mix gain",RiftVoicePolicy.MixGain(2.5f)*2.5f<=.751f&&RiftVoicePolicy.MixGain(.2f)==1);
            var directory="BuildLogs/Audio-v"+RiftData.Version;Directory.CreateDirectory(directory);
            float[] punch=null,blade=null,spell=null;
            for(int kind=0;kind<12;kind++)
            {
                var clip=RiftAudio.PreviewClip(kind);var samples=new float[clip.samples];clip.GetData(samples,0);float peak=0,energy=0;int saturated=0;
                foreach(float s in samples){peak=Math.Max(peak,Math.Abs(s));energy+=s*s;if(Math.Abs(s)>=.8999f)saturated++;}
                Check("Synthesized cue is audible finite and unclipped "+kind,!float.IsNaN(energy)&&energy/samples.Length>.00005f&&peak<.8999f&&saturated==0&&Math.Abs(samples[0])<.001f,clip.name+": peak="+peak+", rms="+Math.Sqrt(energy/samples.Length));
                WriteWave(Path.Combine(directory,kind.ToString("D2")+"-"+clip.name.Replace(' ','-')+".wav"),samples,clip.frequency);
                if(kind==0)punch=samples;if(kind==7)blade=samples;if(kind==8)spell=samples;
                UnityEngine.Object.DestroyImmediate(clip);
            }
            Check("The three hit audio clips have different attack samples",Math.Abs(punch[150]-blade[150])>.005f&&Math.Abs(blade[150]-spell[150])>.005f);
            var suspended=new RiftBattle(3821,0);suspended.Summon(0);var fighter=suspended.Fighters[0];fighter.x=50;fighter.cooldown=0;var victim=Dummy(1000,7,55);victim.stun=999;suspended.Fighters.Add(victim);suspended.Tick(.025f);
            float windup=fighter.windup,pose=fighter.animation;fighter.stun=.5f;suspended.Tick(.3f);
            Check("Stun suspends the windup pose together with its attack clock",windup>0&&pose>.4f&&Math.Abs(fighter.windup-windup)<.001f&&Math.Abs(fighter.animation-pose)<.001f&&victim.health==1000);
            suspended.Tick(.4f);
            Check("A suspended attack lands during its actual strike pose",victim.health<1000&&fighter.animation<=.36f&&fighter.animation>=.1f);
        }
        static void WriteWave(string path,float[] samples,int rate)
        {
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples.Length*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(samples.Length*2);foreach(float s in samples)writer.Write((short)(Mathf.Clamp(s,-1,1)*32767));
            }
        }
    }
}
#endif
