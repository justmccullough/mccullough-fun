using UnityEditor;
using UnityEngine;

// Import settings for the Kenney CC0 models under Resources/Kenney. Clips are imported as legacy animations
// so the game can play them by name from code, and materials are skipped because MooArt builds its own.
public class KenneyImport : AssetPostprocessor
{
    // Bump when these settings change so Unity reimports the models.
    public override uint GetVersion() => 2;

    private bool Ours => assetPath.Replace('\\', '/').Contains("/Resources/Kenney/");

    private void OnPreprocessModel()
    {
        if (!Ours) return;
        var model = (ModelImporter)assetImporter;
        model.animationType = ModelImporterAnimationType.Legacy;
        model.importAnimation = true;
        model.materialImportMode = ModelImporterMaterialImportMode.None;
        model.importCameras = false;
        model.importLights = false;
        model.importBlendShapes = false;
        model.isReadable = false;
    }

    private void OnPreprocessTexture()
    {
        if (!Ours) return;
        var texture = (TextureImporter)assetImporter;
        // The colormaps are tiny palette atlases: keep every swatch crisp.
        texture.mipmapEnabled = false;
        texture.filterMode = FilterMode.Point;
        texture.textureCompression = TextureImporterCompression.Uncompressed;
        texture.wrapMode = TextureWrapMode.Clamp;
        // Readable so MooArt can repaint each hero's hair to match her character.
        texture.isReadable = true;
    }
}
