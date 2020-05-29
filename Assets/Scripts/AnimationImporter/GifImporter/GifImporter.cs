using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using static UniGif;
using UnityEditor.Experimental.AssetImporters;

public class GifImporter : MonoBehaviour {
    [MenuItem("Assets/加载GIF动画")]
    public static void LoadGIF() {
        //读取选中的GIF文件
        foreach (var item in Selection.objects) {
            if (GetExtensionFromPath(AssetDatabase.GetAssetPath(item)) == "gif") {
                GifFile gif = LoadGif(AssetDatabase.GetAssetPath(item));

                //生成动画
                string folderPath = GetFolderFromPath(AssetDatabase.GetAssetPath(item));
                GenerateAnimation(gif.name, gif, folderPath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }

    [MenuItem("Assets/加载GIF动画", true)]
    public static bool LoadGIFValidation() {
        //是否至少有一个选择的物体是GIF
        foreach (var item in Selection.objects) {
            if (GetExtensionFromPath(AssetDatabase.GetAssetPath(item)) == "gif") {
                return true;
            }
        }

        return false;
    }

    //读取GIF文件
    public static GifFile LoadGif(string filePath) {
        var gifData = new GifData();

        int byteIndex = 0;
        byte[] bytes = File.ReadAllBytes(filePath);
        SetGifHeader(bytes, ref byteIndex, ref gifData);

        SetGifBlock(bytes, ref byteIndex, ref gifData);

        var textures = GetTextures(gifData, FilterMode.Point, TextureWrapMode.Clamp);

        string fileName = GetNameFromPath(filePath);

        return new GifFile { name = fileName, textureList = textures };
    }

    //获取GIF贴图列表
    static List<GifTexture> GetTextures(GifData gifData, FilterMode filterMode, TextureWrapMode wrapMode) {
        List<GifTexture> gifTexList = new List<GifTexture>(gifData.m_imageBlockList.Count);
        List<ushort> disposalMethodList = new List<ushort>(gifData.m_imageBlockList.Count);

        int imgIndex = 0;

        for (int i = 0; i < gifData.m_imageBlockList.Count; i++) {
            byte[] decodedData = GetDecodedData(gifData.m_imageBlockList[i]);

            GraphicControlExtension? graphicCtrlEx = GetGraphicCtrlExt(gifData, imgIndex);

            int transparentIndex = GetTransparentIndex(graphicCtrlEx);

            disposalMethodList.Add(GetDisposalMethod(graphicCtrlEx));

            Color32 bgColor;
            List<byte[]> colorTable = GetColorTableAndSetBgColor(gifData, gifData.m_imageBlockList[i], transparentIndex, out bgColor);

            bool filledTexture;
            Texture2D tex = CreateTexture2D(gifData, gifTexList, imgIndex, disposalMethodList, bgColor, filterMode, wrapMode, out filledTexture);

            // Set pixel data
            int dataIndex = 0;
            // Reverse set pixels. because GIF data starts from the top left.
            for (int y = tex.height - 1; y >= 0; y--) {
                SetTexturePixelRow(tex, y, gifData.m_imageBlockList[i], decodedData, ref dataIndex, colorTable, bgColor, transparentIndex, filledTexture);
            }
            tex.Apply();

            float delaySec = GetDelaySec(graphicCtrlEx);

            // Add to GIF texture list
            gifTexList.Add(new GifTexture(tex, delaySec));

            imgIndex++;
        }

        return gifTexList;
    }

    //生成动画
    static void GenerateAnimation(string name, GifFile gif, string path) {
        AnimationClip clip = new AnimationClip();
        clip.name = name;
        clip.frameRate = 25;

        //创建动画文件
        string asset = string.Format("{0}/{1}.anim", path, name);
        AssetDatabase.CreateAsset(clip, asset);

		SpriteImportData[] spriteImportData = new SpriteImportData[0];
        Texture2D atlas = AtlasGenerator.GenerateAtlas(gif, out spriteImportData);
        atlas.filterMode = FilterMode.Point;
        atlas.name = name;
        //AssetDatabase.AddObjectToAsset(atlas, clip);

        byte[] pngBytes = atlas.EncodeToPNG();
        string atlasPath = string.Format("{0}/{1}.png", path, name);
        File.WriteAllBytes(atlasPath, pngBytes);
        AssetDatabase.Refresh();

        Texture2D atlasObject = (Texture2D)AssetDatabase.LoadAssetAtPath(atlasPath, typeof(Texture2D));

        //生成贴图
        List<Sprite> sprites = GenerateSprites(atlasObject, spriteImportData, clip);

        //帧数
        int length = sprites.Count;

        EditorCurveBinding spriteBinding = new EditorCurveBinding();
        spriteBinding.type = typeof(SpriteRenderer);
        spriteBinding.path = "";
        spriteBinding.propertyName = "m_Sprite";

        //设置帧信息
        ObjectReferenceKeyframe[] spriteKeyFrames = new ObjectReferenceKeyframe[length + 1];

        float time = 0;
        for (int i = 0; i < length; i++) {
            ObjectReferenceKeyframe frame = new ObjectReferenceKeyframe();
            frame.time = time;
            frame.value = sprites[i];
            time += gif.textureList[i].m_delaySec;

            spriteKeyFrames[i] = frame;
        }

        //单独设置最后一帧
        float frameTime = 1f / clip.frameRate;
        ObjectReferenceKeyframe lastFrame = new ObjectReferenceKeyframe();
        lastFrame.time = time - frameTime;
        lastFrame.value = sprites[length - 1];
        spriteKeyFrames[spriteKeyFrames.Length - 1] = lastFrame;

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeyFrames);

        //循环设置
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        //循环设置
        //if (animationSetting.loop) {
        //    settings.loopTime = true;
        //    clip.wrapMode = WrapMode.Loop;
        //} else {
        //    settings.loopTime = false;
        //    clip.wrapMode = WrapMode.Once;
        //}

        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(clip));
    }

    //生成Sprite
    static List<Sprite> GenerateSprites(Texture2D texture, SpriteImportData[] spriteImportData, Object parentObject) {
        List<Sprite> sprites = new List<Sprite>();

        for (int i = 0; i < spriteImportData.Length; i++) {
            Sprite sprite = Sprite.Create(texture, spriteImportData[i].rect, new Vector2(.5f, 0), 100, 1, SpriteMeshType.Tight);
            //Sprite名称
            string name = string.Format("{0}_{1}", texture.name, i);
            sprite.name = name;

            AssetDatabase.AddObjectToAsset(sprite, parentObject);

            sprites.Add(sprite);
        }

        return sprites;
    }

    #region 帮助方法

    //从文件路径获取名称
    static string GetNameFromPath(string path) {
        //去除前缀
        var sub = path.Split('/');
        //去除后缀
        var sub2 = sub[sub.Length - 1].Split('.');

        return sub2[0];
    }

    //获取拓展名
    static string GetExtensionFromPath(string path) {
        var sub = path.Split('.');
        return sub[sub.Length - 1];
    }

    //获取文件所在文件夹
    static string GetFolderFromPath(string path) {
        //获取文件部分长度
        var sub = path.Split('/');
        int fileNameLength = sub[sub.Length - 1].Length;

        return path.Substring(0, path.Length - fileNameLength - 1);
    }

    #endregion
}

public struct GifFile {
    public string name;
    public List<GifTexture> textureList;
}
