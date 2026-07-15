package com.denfense.server.service.gacha;

import org.springframework.stereotype.Component;

import java.util.concurrent.ThreadLocalRandom;

@Component
public class DefaultGachaRandomGenerator implements GachaRandomGenerator {
    @Override
    public int nextInt(int bound) {
        if (bound <= 0) {
            throw new IllegalArgumentException("bound must be positive");
        }
        return ThreadLocalRandom.current().nextInt(bound);
    }
}
