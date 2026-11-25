using System;
using Fusion;

namespace MyDefense.Battle
{
    public sealed partial class BattleWaveStateAuthority
    {
        // Cosmetic sequence is deliberately not persistent battle state or a reward/command identifier.
        private ulong _presentationSequence;
        public static event Action<BattleWaveStateAuthority> PresentationReset;
        public static event Action<BattleWaveStateAuthority,string,ulong,int,int,int> PresentationMergeAccepted;
        public static event Action<BattleWaveStateAuthority,string,ulong,NetworkId,ulong,int> PresentationSpawnAccepted;
        private void ResetPresentationEvents(){_presentationSequence=0;try{PresentationReset?.Invoke(this);}catch(Exception error){UnityEngine.Debug.LogException(error);}}
        private void PublishMergePresentation(int player,int source,int target)
        {
            if(Object==null||!Object.IsValid||!HasStateAuthority||Runner==null||!Runner.IsRunning||source==target||_presentationSequence==ulong.MaxValue)return;
            string session=Executor?.PresentationSessionId;if(string.IsNullOrWhiteSpace(session))return;
            try{RPC_PresentationMerge(session,++_presentationSequence,player,source,target);}catch(Exception error){UnityEngine.Debug.LogException(error);}
        }
        public void PublishAcceptedMonsterPresentation(NetworkObject monster,ulong runtimeId)
        {
            if(Object==null||!Object.IsValid||!HasStateAuthority||Runner==null||!Runner.IsRunning||monster==null||!monster.IsValid||runtimeId==0||_presentationSequence==ulong.MaxValue)return;
            string session=Executor?.PresentationSessionId;if(string.IsNullOrWhiteSpace(session))return;
            try{RPC_PresentationSpawn(session,++_presentationSequence,monster.Id,runtimeId,Runner.Tick.Raw);}catch(Exception error){UnityEngine.Debug.LogException(error);}
        }
        [Rpc(RpcSources.StateAuthority,RpcTargets.All,Channel=RpcChannel.Reliable)]
        private void RPC_PresentationMerge(string session,ulong sequence,int player,int source,int target)
        {try{PresentationMergeAccepted?.Invoke(this,session,sequence,player,source,target);}catch(Exception error){UnityEngine.Debug.LogException(error);}}
        [Rpc(RpcSources.StateAuthority,RpcTargets.All,Channel=RpcChannel.Reliable)]
        private void RPC_PresentationSpawn(string session,ulong sequence,NetworkId objectId,ulong runtimeId,int spawnedAtTick)
        {try{PresentationSpawnAccepted?.Invoke(this,session,sequence,objectId,runtimeId,spawnedAtTick);}catch(Exception error){UnityEngine.Debug.LogException(error);}}
    }
}
