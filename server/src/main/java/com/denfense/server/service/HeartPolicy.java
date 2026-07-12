package com.denfense.server.service;

import org.springframework.stereotype.Component;

import java.time.Duration;
import java.time.LocalDateTime;

@Component
public class HeartPolicy {

    public static final int MAX_HEART = 100;
    public static final int RECHARGE_MINUTES = 15;
    public static final int HEARTS_PER_INTERVAL = 10;

    public HeartSnapshot calculate(int currentHeart, LocalDateTime lastUpdateTime) {
        LocalDateTime now = LocalDateTime.now();
        
        if (lastUpdateTime == null) {
            // 초기 상태
            return new HeartSnapshot(MAX_HEART, null, now, now);
        }

        if (currentHeart >= MAX_HEART) {
            return new HeartSnapshot(currentHeart, null, now, now);
        }

        long minutesPassed = Duration.between(lastUpdateTime, now).toMinutes();

        if (minutesPassed >= RECHARGE_MINUTES) {
            int intervals = (int) (minutesPassed / RECHARGE_MINUTES);
            int earnedHearts = intervals * HEARTS_PER_INTERVAL;
            int newHeart = currentHeart + earnedHearts;

            if (newHeart >= MAX_HEART) {
                return new HeartSnapshot(MAX_HEART, null, now, now);
            } else {
                LocalDateTime effectiveUpdateTime = lastUpdateTime.plusMinutes((long) intervals * RECHARGE_MINUTES);
                LocalDateTime nextRecoveryAt = effectiveUpdateTime.plusMinutes(RECHARGE_MINUTES);
                return new HeartSnapshot(newHeart, nextRecoveryAt, effectiveUpdateTime, now);
            }
        } else {
            LocalDateTime nextRecoveryAt = lastUpdateTime.plusMinutes(RECHARGE_MINUTES);
            return new HeartSnapshot(currentHeart, nextRecoveryAt, lastUpdateTime, now);
        }
    }
}
