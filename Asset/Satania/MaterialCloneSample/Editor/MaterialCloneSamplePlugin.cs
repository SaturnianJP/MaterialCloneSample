﻿//#define DEBUG

using System.Collections.Generic;
using nadena.dev.ndmf;
using satania;
using UnityEngine;
using UnityEditor;

[assembly: ExportsPlugin(typeof(MaterialCloneSamplePlugin))]
namespace satania
{
    public class MaterialCloneSamplePlugin : Plugin<MaterialCloneSamplePlugin>
    {
        public override string DisplayName => "MaterialCloneSample";
        public override string QualifiedName => "satania.MaterialCloneSample";

        private const string LightLimitChangerQualifiedName = "io.github.azukimochi.light-limit-changer";
        
        private const string AvatarOptimizerQualifiedName = "com.anatawa12.avatar-optimizer";

        public Dictionary<Material, Material> duplicatedMaterials = new Dictionary<Material, Material>();

        public bool DuplicateMaterials(BuildContext ctx, Renderer[] renderers, out Dictionary<Material, Material> dupMats)
        {
            dupMats = new Dictionary<Material, Material>();

            if (renderers == null || renderers.Length < 1)
                return false;

            for (int j = 0; j < renderers.Length; j++)
            {
                var newMats = new Material[renderers[j].sharedMaterials.Length];

                for (int i = 0; i < renderers[j].sharedMaterials.Length; i++)
                {
                    var mat = renderers[j].sharedMaterials[i];
                    if (mat == null)
                        continue;

                    if (!dupMats.ContainsKey(mat))
                    {
                        Material cloned = GameObject.Instantiate(mat);

                        //NDMF側に変更したことを登録
                        ObjectRegistry.RegisterReplacedObject(mat, cloned);

                        dupMats.Add(mat, cloned);

                        //NDMFのAssetコンテナに追加
                        AssetDatabase.AddObjectToAsset(cloned, ctx.AssetContainer);
                    }
                    newMats[i] = dupMats[mat];
                }
                renderers[j].sharedMaterials = newMats;
                EditorUtility.SetDirty(renderers[j]);
            }

            return true;
        }

        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming)
                .BeforePlugin(LightLimitChangerQualifiedName)
                .BeforePlugin(AvatarOptimizerQualifiedName)
                .Run("Material Clone Process", ctx =>
                {
                    MaterialCloneSample[] comps = ctx.AvatarRootObject.GetComponentsInChildren<MaterialCloneSample>();
                    MaterialCloneSample comp = comps.Length > 0 ? comps[0] : null;

                    //アバター内にCloneSampleがなかったら返す
                    if (comp == null)
                        return;

                    //2個以上ある場合は削除
                    if (comps.Length > 1)
                    {
                        for (int i = 1; i < comps.Length; i++)
                        {
#if DEBUG
                            Debug.Log($"Deleting {comps[i].name}");
#endif
                            GameObject.DestroyImmediate(comps[i]);
                        }
                    }

                    if (comp != null)
                    {
                        SkinnedMeshRenderer[] skinnedMeshs = ctx.AvatarRootObject.GetComponentsInChildren<SkinnedMeshRenderer>();

                        //マテリアルを複製してライトの設定を上書き
                        if (DuplicateMaterials(ctx, skinnedMeshs, out Dictionary<Material, Material> mats))
                        {
                            //複製したマテリアルに対して何かしらの変更処理
                        }
                        GameObject.DestroyImmediate(comp);
                    }
                });
        }
    }
}

