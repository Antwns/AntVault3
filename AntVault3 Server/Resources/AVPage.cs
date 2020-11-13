using System;
using System.Drawing;

namespace AntVault3_Server.Resources
{
    [Serializable()]
    public class AVPage
    {
        public Bitmap Banner { get; set; }

        public byte[] Content { get; set; }
    }
}
