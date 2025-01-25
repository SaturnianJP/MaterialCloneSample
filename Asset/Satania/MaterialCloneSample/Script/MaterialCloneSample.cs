using System.Collections.Generic;
using nadena.dev.ndmf;
using satania;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[assembly: ExportsPlugin(typeof(MaterialCloneSample_Plugin))]
namespace satania
{
    public class MaterialCloneSample : MonoBehaviour { }

#if UNITY_EDITOR
    public class MaterialCloneSample_Plugin : Plugin<MaterialCloneSample_Plugin>
    {
        public Dictionary<string, Material> duplicatedMaterials = new Dictionary<string, Material>();

        public bool DuplicateMaterials(BuildContext ctx, SkinnedMeshRenderer[] skinnedMeshs, out Dictionary<string, Material> dupMats)
        {
            Debug.Log($"{ctx.AvatarRootObject.name}");

            dupMats = new Dictionary<string, Material>();

            if (skinnedMeshs == null || skinnedMeshs.Length < 1)
                return false;

            for (int j = 0; j < skinnedMeshs.Length; j++)
            {
                var newMats = new Material[skinnedMeshs[j].sharedMaterials.Length];

                for (int i = 0; i < skinnedMeshs[j].sharedMaterials.Length; i++)
                {
                    var mat = skinnedMeshs[j].sharedMaterials[i];
                    if (mat == null)
                        continue;

                    //MainAssetな場合
                    if (AssetDatabase.IsMainAsset(mat))
                    {
                        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mat));
                        
                        //まだ複製してないマテリアルなら複製
                        if (!dupMats.ContainsKey(guid))
                        {
                            Material cloned = GameObject.Instantiate(mat);
                            dupMats.Add(guid, cloned);
                            AssetDatabase.AddObjectToAsset(cloned, ctx.AssetContainer);
                                
                            Debug.Log($"Duplicated -> {cloned}({guid})");
                        }
                        newMats[i] = dupMats[guid];
                    }
                }
                skinnedMeshs[j].sharedMaterials = newMats;
                EditorUtility.SetDirty(skinnedMeshs[j]);
            }

            return true;
        }

        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming)
                .AfterPlugin("nadena.dev.modular-avatar")
                .Run("satania.materialclonesample", ctx =>
                {
    #region コンポーネントが被ってたら削除
                    MaterialCloneSample[] comps = ctx.AvatarRootObject.GetComponentsInChildren<MaterialCloneSample>();
                    MaterialCloneSample comp = comps[0];

                    if (comps.Length > 1)
                    {
                        for (int i = 1; i < comps.Length; i++)
                        {
                            Debug.Log($"Deleting {comps[i].name}");
                            GameObject.DestroyImmediate(comps[i]);
                        }
                    }
    #endregion

    #region メイン処理
                    if (comp != null)
                    {
                        SkinnedMeshRenderer[] skinnedMeshs = ctx.AvatarRootObject.GetComponentsInChildren<SkinnedMeshRenderer>();

                        //マテリアルを複製してライトの設定を上書き
                        if (DuplicateMaterials(ctx, skinnedMeshs, out Dictionary<string, Material> mats))
                        {
                            //複製したマテリアルに対して何かしらの変更処理
                        }
                        GameObject.DestroyImmediate(comp);
                    }
    #endregion
                });
        }
    }
#endif
}

