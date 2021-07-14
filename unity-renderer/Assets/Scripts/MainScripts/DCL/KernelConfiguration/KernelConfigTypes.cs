using System;

namespace KernelConfigurationTypes
{
    [Serializable]
    public class Features
    {
        public bool enableBuilderInWorld = false;
        public bool enableAvatarLODs = false;

        public bool Equals(Features other) { return enableBuilderInWorld == other?.enableBuilderInWorld; }

        public Features Clone()
        {
            Features clone = (Features) this.MemberwiseClone();
            return clone;
        }
    }

    [Serializable]
    public class Comms
    {
        public float commRadius = 4;
        public bool voiceChatEnabled = false;

        public bool Equals(Comms other)
        {
            return commRadius == other?.commRadius &&
                   voiceChatEnabled == other?.voiceChatEnabled;
        }

        public Comms Clone()
        {
            Comms clone = (Comms) this.MemberwiseClone();
            return clone;
        }
    }

    [Serializable]
    public class Profiles
    {
        public string nameValidCharacterRegex = "";
        public string nameValidRegex = "";

        public bool Equals(Profiles other)
        {
            return nameValidCharacterRegex == other?.nameValidCharacterRegex &&
                   nameValidRegex == other?.nameValidRegex;
        }

        public Profiles Clone()
        {
            Profiles clone = (Profiles) this.MemberwiseClone();
            return clone;
        }
    }

    [Serializable]
    public class WorldRange
    {
        public int xMin = 0;
        public int xMax = 0;
        public int yMin = 0;
        public int yMax = 0;

        public bool Equals(WorldRange other)
        {
            return xMin == other?.xMin &&
                   xMax == other?.xMax &&
                   yMin == other?.yMin &&
                   yMax == other?.yMax;
        }

        public WorldRange Clone()
        {
            WorldRange clone = (WorldRange) this.MemberwiseClone();
            return clone;
        }
        public WorldRange(int xMin, int yMin, int xMax, int yMax)
        {
            this.xMin = xMin;
            this.yMin = yMin;
            this.xMax = xMax;
            this.yMax = yMax;
        }

        public bool Contains(int x, int y)
        {
            return x >= xMin && x <= xMax &&
                   y >= yMin && y <= yMax;
        }
    }
}