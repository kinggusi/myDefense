package com.denfense.server.service.gacha;

import org.springframework.stereotype.Component;
import com.denfense.server.service.random.EconomyRandomSource;
import lombok.RequiredArgsConstructor;

@Component
@RequiredArgsConstructor
public class DefaultGachaRandomGenerator implements GachaRandomGenerator {
    private final EconomyRandomSource random;

    @Override
    public int nextInt(int bound) {
        if (bound <= 0) {
            throw new IllegalArgumentException("bound must be positive");
        }
        return random.nextInt(bound);
    }
}
