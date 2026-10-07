using UnityEditor;
using UnityEngine;

namespace Runeheir.EditorTools
{
    /// <summary>
    /// Import settings for the Blender-built characters in Resources/Characters (Tools/Blender/build_characters.py):
    /// meters at scale 1, Blender's axes baked into the vertices (no -90° root rotation), every bone kept as a Transform
    /// for the procedural animation, and no Animator (RiggedBody drives the bones; give the model an Animator Controller
    /// later and CharacterAnimationBridge plays clips instead).
    /// </summary>
    public sealed class CharacterModelPostprocessor : AssetPostprocessor
    {
        public const string Folder = "Assets/_Runeheir/Resources/Characters/";

        public override uint GetVersion()
        {
            return 2;
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

            // Generic keeps the skinning (None would import a static mesh); the Animator it adds is removed below.
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
            importer.optimizeGameObjects = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.None;
            importer.weldVertices = false;
            importer.skinWeights = ModelImporterSkinWeights.Standard;

            // The game paints each material slot (Cloth, Metal, Skin...) through the toon shader by name; the imported
            // materials only carry those names.
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        }

        private void OnPostprocessModel(GameObject root)
        {
            if (!assetPath.Replace('\\', '/').StartsWith(Folder))
            {
                return;
            }

            // An Animator without a controller would make CharacterAnimationBridge skip the procedural skill motions.
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
            {
                Object.DestroyImmediate(animator);
            }
        }
    }
}
