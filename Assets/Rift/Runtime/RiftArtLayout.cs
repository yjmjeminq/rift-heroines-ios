using System;

namespace RiftHeroines
{
    [Serializable]
    public sealed class RiftArtLayout
    {
        public string version;
        public int columns, rows;
        public RiftHeroArt[] heroes;
    }
    [Serializable]
    public sealed class RiftHeroArt
    {
        public string id;
        public int width, height;
        public float[] frameFooters;
    }
}
