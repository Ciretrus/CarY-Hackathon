using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class ComicFrame
{
    public CanvasGroup canvasGroup;
    public AudioClip audioClip;
}

[Serializable]
public class ComicPage
{
    public RectTransform pageRect;
    public List<ComicFrame> frames = new List<ComicFrame>();
}
