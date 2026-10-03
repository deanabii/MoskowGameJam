using System;
using System.Collections.Generic;

namespace PhotoModeSystem
{
    [Serializable]
    public class ObjectPhotoEntry
    {
        public string objectName;
        public float valueAmount;

        public ObjectPhotoEntry() { }

        public ObjectPhotoEntry(string name, float value)
        {
            objectName = name;
            valueAmount = value;
        }
    }

    [Serializable]
    public class PhotoData
    {
        public string photoId;
        public string fileName;
        public string timestamp;
        public List<string> visibleObjectNames = new List<string>();
        public List<ObjectPhotoEntry> visibleObjects = new List<ObjectPhotoEntry>();
        public float totalValue;

        public PhotoData()
        {
            photoId = Guid.NewGuid().ToString();
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
