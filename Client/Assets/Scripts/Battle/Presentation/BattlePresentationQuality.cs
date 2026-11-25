using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MyDefense.Battle.Presentation
{
    /// <summary>Temporary Built-in Forward AA lease. Restores global/camera settings when the last Battle owner leaves.</summary>
    public sealed class BattlePresentationQuality : MonoBehaviour
    {
        private sealed class CameraLease {public bool Original;public int Owners;}
        private static readonly HashSet<BattlePresentationQuality> Owners=new();
        private static readonly Dictionary<Camera,CameraLease> Cameras=new();
        private static int _originalAa,_appliedAa;
        private Camera _camera;
        public bool OwnsScope=>Owners.Contains(this);
        public void BeginScope(Camera camera)
        {
            if(camera==null||GraphicsSettings.currentRenderPipeline!=null||camera.actualRenderingPath!=RenderingPath.Forward)return;
            if(Owners.Add(this)&&Owners.Count==1)
            {_originalAa=QualitySettings.antiAliasing;_appliedAa=Mathf.Max(4,_originalAa);QualitySettings.antiAliasing=_appliedAa;}
            if(_camera==camera)return;
            DetachCamera();_camera=camera;
            if(!Cameras.TryGetValue(camera,out CameraLease lease)){lease=new CameraLease{Original=camera.allowMSAA};Cameras.Add(camera,lease);}
            lease.Owners++;camera.allowMSAA=true;
        }
        private void DetachCamera()
        {
            if(ReferenceEquals(_camera,null))return;
            if(Cameras.TryGetValue(_camera,out CameraLease lease)&&--lease.Owners==0)
            {if(_camera!=null)_camera.allowMSAA=lease.Original;Cameras.Remove(_camera);}
            _camera=null;
        }
        public void EndScope()
        {
            DetachCamera();
            if(Owners.Remove(this)&&Owners.Count==0&&QualitySettings.antiAliasing==_appliedAa)QualitySettings.antiAliasing=_originalAa;
        }
        private void OnDisable()=>EndScope();
        private void OnDestroy()=>EndScope();
    }
}
