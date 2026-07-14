package com.denfense.server.service.reward;

public interface GameRewardPolicy {
    GameReward calculate(GameRewardContext context);
}
