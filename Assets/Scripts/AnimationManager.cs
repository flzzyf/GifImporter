using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AnimationManager")]
public class AnimationManager : ScriptableObject {
    public List<AnimationInfo> animations;
}

public struct AnimationInfo {
    public AnimationFileType type;

    public string animationFolder;
}

public enum AnimationFileType { Ase, GIF }