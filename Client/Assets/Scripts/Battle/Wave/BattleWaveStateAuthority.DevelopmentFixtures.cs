#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Fusion;
using UnityEngine;
using MyDefense.Battle.Runtime;

namespace MyDefense.Battle
{
    public sealed partial class BattleWaveStateAuthority
    {
        public void RequestDevelopmentMythicFixture(long alienId, string mutationType)
        {
            if (!IsSpawnedForAccess || Object == null || !Object.IsValid || Runner == null)
                return;

            if (!TryResolvePlayerSlot(Runner.LocalPlayer, out int playerSlot))
                return;

            if (HasStateAuthority)
            {
                ApplyDevelopmentMythicFixture(playerSlot, alienId, mutationType);
                return;
            }

            NetworkString<_16> networkMutation = mutationType;
            RPC_RequestDevelopmentMythicFixture(alienId, networkMutation);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestDevelopmentMythicFixture(
            long alienId,
            NetworkString<_16> mutationType,
            RpcInfo info = default)
        {
            if (TryResolvePlayerSlot(info.Source, out int playerSlot))
                ApplyDevelopmentMythicFixture(playerSlot, alienId, mutationType.ToString());
        }

        private void ApplyDevelopmentMythicFixture(int playerSlot, long alienId, string mutationType)
        {
            if (!HasStateAuthority || _executor == null || IsMythicChoiceActive(playerSlot))
                return;

            if (!DevelopmentMythicFixtureRules.TryNormalize(
                    alienId, mutationType, out string normalizedMutation, out string reason))
            {
                Debug.LogWarning($"[P1 Fixture] Rejected: {reason}");
                return;
            }

            if (normalizedMutation != DevelopmentMythicFixtureRules.NoneMutation
                && !_executor.TryGetCanonicalMutationSpec(normalizedMutation, out _))
            {
                Debug.LogWarning($"[P1 Fixture] Rejected: canonical Mutation {normalizedMutation} is unavailable.");
                return;
            }

            NetworkArray<NetworkBool> board = playerSlot == 1 ? Player1BoardOccupied : Player2BoardOccupied;
            int slotIndex = FindFirstEmptyBoardSlot(board);
            if (slotIndex < 0)
            {
                Debug.LogWarning($"[P1 Fixture] Rejected: player {playerSlot} board is full.");
                return;
            }

            NetworkArray<long> alienIds = playerSlot == 1 ? Player1BoardAlienIds : Player2BoardAlienIds;
            NetworkArray<byte> grades = playerSlot == 1 ? Player1BoardGrades : Player2BoardGrades;
            NetworkArray<NetworkString<_16>> mutationTypes =
                playerSlot == 1 ? Player1BoardMutationTypes : Player2BoardMutationTypes;
            NetworkArray<byte> mutationStates =
                playerSlot == 1 ? Player1BoardMutationStates : Player2BoardMutationStates;
            NetworkArray<byte> mutationRerolls =
                playerSlot == 1 ? Player1BoardMutationRerollCounts : Player2BoardMutationRerollCounts;

            bool pureMythic = normalizedMutation == DevelopmentMythicFixtureRules.NoneMutation;
            board.Set(slotIndex, true);
            alienIds.Set(slotIndex, alienId);
            grades.Set(slotIndex, 4);
            mutationTypes.Set(slotIndex, pureMythic ? default : normalizedMutation);
            mutationStates.Set(slotIndex, pureMythic ? MutationStateNone : MutationStateActive);
            mutationRerolls.Set(slotIndex, 0);

            RPC_KidnapApplied(playerSlot, slotIndex, alienId);
            Debug.Log($"[P1 Fixture] Spawned Mythic: player={playerSlot}, slot={slotIndex}, alienId={alienId}, mutation={normalizedMutation}.");
        }
    }
}
#endif
