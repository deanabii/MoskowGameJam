using System;
using System.Collections.Generic;

namespace PhotoModeSystem
{
    [Serializable]
    public class PhotoData
    {
        public string photoId;
        public string fileName;
        public string timestamp;
        public List<string> visibleObjectNames = new List<string>();
        public float totalValue;

        public PhotoData()
        {
            photoId = Guid.NewGuid().ToString();
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
