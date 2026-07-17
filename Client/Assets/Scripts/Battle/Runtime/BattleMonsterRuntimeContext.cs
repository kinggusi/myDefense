using System;
using UnityEngine;

namespace MyDefense.Battle.Runtime
{
    [DisallowMultipleComponent]
    public sealed class BattleMonsterRuntimeContext : MonoBehaviour
    {
        private BattleMonsterRuntimeIdentity _identity;

        public bool IsInitialized => _identity != null;

        public BattleMonsterRuntimeIdentity Identity
        {
            get
            {
                if (_identity == null)
                    throw new InvalidOperationException("Monster runtime context has not been initialized.");
                return _identity;
            }
        }

        public void Initialize(BattleMonsterRuntimeIdentity identity)
        {
            if (_identity != null)
                throw new InvalidOperationException("Monster runtime context can only be initialized once.");

            _identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }
    }
}
