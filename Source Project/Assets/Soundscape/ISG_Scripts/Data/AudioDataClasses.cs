using System;
using System.Collections.Generic;

// Supporting classes for serialization
[Serializable]
public class AudioLibrary
{
    public int monoSources;
    public int stereoSources;
    public int ambisonicSources;
    public string rootFolderPath; 
    public List<AudioCategory> categories;
}

[Serializable]
public class AudioCategory
{
    public string categoryName;
    public List<AudioItem> audioItems = new List<AudioItem>();
}

[Serializable]
public class AudioItem
{
    public string displayName;
    public string audioFilePath;
    public string parametersFilePath;
    public List<Parameter> parameters = new List<Parameter>();
}

[Serializable]
public class Parameter
{
    public string key;
    public float minValue;
    public float maxValue;
    public float defaultValue; // Optional: default value within the range
}
