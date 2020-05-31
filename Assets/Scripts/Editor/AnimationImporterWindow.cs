using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class AnimationImporterWindow : EditorWindow {
	const string title = "人物动画管理器";

	[MenuItem("Window/" + title)]
	public static void ShowWindow() {
		var window = GetWindow<AnimationImporterWindow>(title);

		//设置窗口位置
		float width = 600;
		float height = 500;
		float x = (Screen.currentResolution.width - width) / 2;
		float y = (Screen.currentResolution.height - height) / 2;

		window.position = new Rect(x, y, width, height);

		window.Show();
	}

	private void OnGUI() {
		//标题
		GUIStyle headerStyle = new GUIStyle {
			fontSize = 40,
			alignment = TextAnchor.MiddleCenter
		};
		GUILayout.Label(title, headerStyle, GUILayout.Height(80));

		gifObject = EditorGUILayout.ObjectField("地图贴图Psb文件", gifObject, typeof(Object), false);

		if (GUILayout.Button("qwe")) {
			GifFile gifFile = GifImporter.LoadGif(AssetDatabase.GetAssetPath(gifObject));
			string path = "Assets/qwe";
			Texture2D texture = gifFile.textureList[0].m_texture2d;
			AssetDatabase.CreateAsset(texture, path);
			Texture2D texture2 = gifFile.textureList[1].m_texture2d;
			AssetDatabase.AddObjectToAsset(texture2, texture);

			AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(texture2));

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
	}

	Object gifObject;

	#region 帮助方法

	//换行
	void Separate(int num) {
		for (int i = 0; i < num; i++) {
			EditorGUILayout.Separator();
		}
	}

    #endregion
}
