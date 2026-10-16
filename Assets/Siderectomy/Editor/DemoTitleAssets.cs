using UnityEditor;

public static class DemoTitleAssets {
 public static void Prepare(){const string path="Assets/Siderectomy/Resources/Title/Artwork_20261011.png";var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)return;if(importer.textureType!=TextureImporterType.Default||importer.mipmapEnabled||importer.maxTextureSize!=4096){importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;importer.maxTextureSize=4096;importer.sRGBTexture=true;importer.SaveAndReimport();}}
}
