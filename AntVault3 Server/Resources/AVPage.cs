using System;
using System.Drawing;
using System.Windows.Controls;

namespace AntVault3_Server.Resources
{
    [Serializable]
    public class AVPage
    {
        public Bitmap Banner { get; set; }

        public RichTextBox Content { get; set; }
    }
}
