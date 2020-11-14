using System;
using System.Drawing;

namespace AntVault3_Common
{
    [Serializable]
    public class AVPage
    {
        public Bitmap Banner { get; set; }

        public byte[] Content { get; set; }
    }
}