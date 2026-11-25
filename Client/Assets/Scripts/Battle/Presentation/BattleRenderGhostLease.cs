using System.Collections.Generic;
using UnityEngine;

namespace MyDefense.Battle.Presentation
{
    /// <summary>Owns only a render preview's temporary materials and retained pose meshes.</summary>
    public sealed class BattleRenderGhostLease : MonoBehaviour
    {
        private readonly List<Sprite> _poses=new();
        private readonly List<Material> _materials=new();
        public void Initialize(IEnumerable<Sprite> poses,Transform source)
        {
            foreach(Sprite pose in poses){BattleUnitPoseCache.Retain(pose);_poses.Add(pose);}
            var map=new Dictionary<Material,Material>();
            Renderer[] originals=source.GetComponentsInChildren<Renderer>(true),copies=GetComponentsInChildren<Renderer>(true);
            for(int i=0;i<copies.Length;i++)
            {
                if(copies[i] is LineRenderer line&&line.useWorldSpace)
                {
                    var points=new Vector3[line.positionCount];line.GetPositions(points);
                    for(int p=0;p<points.Length;p++)points[p]=line.transform.InverseTransformPoint(points[p]);
                    line.useWorldSpace=false;line.SetPositions(points);
                }
                Material[] shared=copies[i].sharedMaterials;
                for(int m=0;m<shared.Length;m++)
                {
                    if(shared[m]==null)continue;
                    if(!map.TryGetValue(shared[m],out Material copy)){copy=new Material(shared[m]);map.Add(shared[m],copy);_materials.Add(copy);}
                    shared[m]=copy;
                }
                copies[i].sharedMaterials=shared;
                if(i<originals.Length){var properties=new MaterialPropertyBlock();originals[i].GetPropertyBlock(properties);properties.SetFloat("_PresentationDim",1);copies[i].SetPropertyBlock(properties);}
            }
        }
        public void ReleaseResources()
        {
            foreach(Sprite pose in _poses)BattleUnitPoseCache.Release(pose);_poses.Clear();
            foreach(Material material in _materials)BattleSpriteGeometry.Release(material);_materials.Clear();
        }
        private void OnDestroy()=>ReleaseResources();
    }
}
