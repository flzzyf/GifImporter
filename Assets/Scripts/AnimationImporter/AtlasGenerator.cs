using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor.Experimental.AssetImporters;

public class AtlasGenerator {
    public static Texture2D GenerateAtlas(GifFile gif, out SpriteImportData[] spriteData) {
        int spriteCount = gif.textureList.Count;

        var cols = spriteCount;
        var rows = 1;

        var divider = 2;

		Texture2D sampleTexture = gif.textureList[0].m_texture2d;
		Vector2Int spriteSize = new Vector2Int(sampleTexture.width, sampleTexture.height);
        var width = cols * spriteSize.x;
        var height = rows * spriteSize.y;

		while (width > height) {
			cols = (int)Math.Ceiling((float)spriteCount / divider);
			rows = (int)Math.Ceiling((float)spriteCount / cols);

			width = cols * spriteSize.x;
			height = rows * spriteSize.y;

			if (cols <= 1) {
				break;
			}

			divider++;
		}

		if (height > width)
			divider -= 2;
		else
			divider -= 1;

		if (divider < 1)
			divider = 1;

		cols = (int)Math.Ceiling((float)spriteCount / divider);
		rows = (int)Math.Ceiling((float)spriteCount / cols);

		Texture2D[] textures = new Texture2D[spriteCount];
		for (int i = 0; i < gif.textureList.Count; i++) {
			textures[i] = gif.textureList[i].m_texture2d;
		}

		return GenerateAtlas(textures, out spriteData, spriteSize, cols, rows);
	}

	static Texture2D GenerateAtlas(Texture2D[] textures, out SpriteImportData[] spriteData, Vector2Int spriteSize, int cols, int rows) {
		var spriteImportData = new List<SpriteImportData>();

		var width = cols * spriteSize.x;
		var height = rows * spriteSize.y;

		var atlas = CreateTransparentTexture(width, height);
		var index = 0;

		for (var row = 0; row < rows; row++) {
			for (var col = 0; col < cols; col++) {
				Rect spriteRect = new Rect(col * spriteSize.x, atlas.height - ((row + 1) * spriteSize.y), spriteSize.x, spriteSize.y);
				atlas.SetPixels((int)spriteRect.x, (int)spriteRect.y, (int)spriteRect.width, (int)spriteRect.height, textures[index].GetPixels());
				atlas.Apply();

				var importData = new SpriteImportData {
					rect = spriteRect,
					pivot = new Vector2(.5f, 0),
					border = Vector4.zero,
					name = index.ToString()
				};

				spriteImportData.Add(importData);

				index++;
				if (index >= textures.Length)
					break;
			}
			if (index >= textures.Length)
				break;
		}

		spriteData = spriteImportData.ToArray();
		return atlas;
	}

	//创建透明贴图
	static Texture2D CreateTransparentTexture(int width, int height) {
		Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
		Color[] pixels = new Color[width * height];

		for (int i = 0; i < pixels.Length; i++)
			pixels[i] = Color.clear;

		texture.SetPixels(pixels);
		texture.Apply();

		return texture;
	}
}
