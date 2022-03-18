using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnboundLib.GameModes
{
    public struct UISettings
    {
        public readonly string description;
        public readonly int descriptionFontSize;
        public readonly string videoURL;

        public UISettings(string description = "", int descriptionFontSize = 30, string videoURL = "https://media.giphy.com/media/50dtBlALJ5jIgmnasA/giphy.mp4")
        {
            this.description = description;
            this.descriptionFontSize = descriptionFontSize;
            this.videoURL = videoURL;
        }
    }
}
