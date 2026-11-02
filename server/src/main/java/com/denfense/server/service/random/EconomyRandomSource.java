package com.denfense.server.service.random;

import org.springframework.stereotype.Component;

import java.security.SecureRandom;

/** Server-only, cryptographically strong bounded draws for persistent economy rewards. */
@Component
public class EconomyRandomSource {
    private final SecureRandom random = new SecureRandom();

    public int nextInt(int bound) {
        if (bound <= 0) throw new IllegalArgumentException("bound must be positive");
        return random.nextInt(bound);
    }
}
