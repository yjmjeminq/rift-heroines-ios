using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiftHeroines
{
    [Serializable]
    public sealed class RiftRosterIdentity
    {
        public string id,displayName,canonicalName,originalName,franchise,sourceUrl,sourceName,appearance;
        public int faction;
    }
    [Serializable]
    public sealed class RiftRosterCatalog
    {
        public string version,checkedDate;
        public RiftRosterIdentity[] characters;

        public static RiftRosterCatalog LoadValidated()
        {
            var file=Resources.Load<TextAsset>("Rift/canonical-roster");
            if(!file)throw new InvalidOperationException("Missing verified canonical roster");
            var catalog=JsonUtility.FromJson<RiftRosterCatalog>(file.text);
            if(catalog==null)throw new InvalidOperationException("Invalid canonical roster JSON");
            catalog.Validate(RiftData.Heroes);return catalog;
        }
        public void Validate(HeroDefinition[] heroes)
        {
            if(version!=RiftData.Version || characters==null || heroes==null || characters.Length!=RiftData.HeroCount || heroes.Length!=RiftData.HeroCount)
                throw new InvalidOperationException("Canonical roster version or size mismatch");
            var used=new HashSet<string>(StringComparer.Ordinal);var factionCounts=new int[2];
            for(int i=0;i<heroes.Length;i++)
            {
                var identity=characters[i];var hero=heroes[i];Uri source;
                if(identity==null || hero==null || identity.id!=hero.id || identity.displayName!=hero.name || identity.faction!=hero.faction || !used.Add(identity.id))
                    throw new InvalidOperationException("Unverified or mismatched playable character at slot "+i);
                if(string.IsNullOrEmpty(identity.canonicalName) || string.IsNullOrEmpty(identity.originalName) || string.IsNullOrEmpty(identity.sourceName) ||
                    !Uri.TryCreate(identity.sourceUrl,UriKind.Absolute,out source) || source.Scheme!="https")
                    throw new InvalidOperationException("Missing canonical identity/source for "+hero.id);
                bool official=identity.faction==0 ? source.Host=="naruto-official.com" : source.Host=="bleach-ros.bn-ent.net" || source.Host=="www.bleach-bravesouls.com";
                if(!official || identity.franchise!=(identity.faction==0?"NARUTO":"BLEACH"))
                    throw new InvalidOperationException("Unexpected character source/franchise for "+hero.id);
                factionCounts[identity.faction]++;
            }
            if(factionCounts[0]!=RiftData.HeroesPerFaction || factionCounts[1]!=RiftData.HeroesPerFaction)
                throw new InvalidOperationException("Each franchise must have six playable characters");
        }
    }
}
