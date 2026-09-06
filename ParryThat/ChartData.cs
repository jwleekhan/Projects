using System;
using UnityEngine;

[Serializable]
public class NoteData
{
    public int direction;

    public float noticeBeat;
    public float arriveBeat;

    public int type;
}

[Serializable]
public class ChartData
{
    public float bpm;

    public int strikerType;
    public float startBeat;
    public float endBeat;

    public NoteData[] notes;
}

public class JsonReader
{
    public static T ReadJson<T>(TextAsset jsonFile)
    {
        string json = jsonFile.text;
        return JsonUtility.FromJson<T>(json);
    }
}