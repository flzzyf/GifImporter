using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(menuName = "AnimationManager")]
public class AnimationManager : ScriptableObject {
    //动画列表
    public List<AnimationInfo> animationInfos = new List<AnimationInfo>();

    //动画ID对应字典
    Dictionary<string, int> animationIndexDic = new Dictionary<string, int>();

    //获取动画管理器实例
    public static  AnimationManager instance {
        get {
            return Resources.FindObjectsOfTypeAll<AnimationManager>()[0];
        }
    }

    //添加动画到表
    public void AddAnimation(AnimationInfo info) {
        //检查是否已有该名称的动画，有则更新
        if (animationIndexDic.ContainsKey(info.name)) {
            animationInfos[animationIndexDic[info.name]] = info;
        } else {
            //没有则添加
            int index = animationInfos.Count;
            animationInfos.Add(info);
            animationIndexDic.Add(info.name, index);
        }
    }

    //更新动画，检查失效的
    public void UpdateAnimations() {

    }
}

//动画信息
[Serializable]
public struct AnimationInfo {
    public string name;

    //类型
    public AnimationFileType type;

    //源文件
    public UnityEngine.Object source;
    //动画
    public AnimationClip animationClip;
    //贴图
    public Texture2D texture;
}

public enum AnimationFileType { Ase, GIF }