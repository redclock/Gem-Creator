using System;

namespace RedGame.GemCreator
{
    [Serializable]
    public class GemSetting
    {
        public int borderCount = 3;
        public int bevelIter = 1;
        public float bevelFactor = 0.1f;
        public float innerLen = 0.1f;
        public float innerHeight = 0.1f;
        public float scaleWidth = 1.0f;
        public float smoothDistance = 0.1f;
        public float smoothPower = 1.0f;
    }
}