package com.denfense.server.balance;

public record InjectorPoolBalance(
        String poolId,
        String poolName,
        boolean poolActive,
        String mutationType,
        int weight,
        String resultType) {
}
