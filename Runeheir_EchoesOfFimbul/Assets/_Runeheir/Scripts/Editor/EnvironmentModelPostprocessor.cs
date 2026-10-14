using UnityEditor;

namespace Runeheir.EditorTools
{
    /// <summary>
    /// Import settings for the Blender-built scenery in Resources/Environment (Tools/Blender/build_environment.py): static
    /// meshes in meters with Blender's axes baked in, the authored normals kept (rocks are faceted, canopies smooth), no
    /// rig or animation, and materials that only carry their slot names (Bark, Leaf, Rock...), which the game paints per
    /// map theme through the toon shader.
    /// </summary>
    public sealed class EnvironmentModelPostprocessor : AssetPostprocessor
    {
        public const string Folder = "Assets/_Runeheir/Resources/Environment/";

        public override uint GetVersion()
        {
            return 1;
        }

        private void OnPreprocessModel()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(Folder))
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.importBlendShapes = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.isReadable = false;
            importer.addCollider = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.None;
            importer.weldVertices = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        }
    }
}
