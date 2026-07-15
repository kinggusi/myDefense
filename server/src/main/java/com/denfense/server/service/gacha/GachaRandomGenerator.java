package com.denfense.server.service.gacha;

public interface GachaRandomGenerator {
    /**
     * Returns a pseudorandom, uniformly distributed int value
     * between 0 (inclusive) and the specified value (exclusive).
     *
     * @param bound the upper bound (exclusive). Must be positive.
     * @return the next pseudorandom, uniformly distributed int value
     * @throws IllegalArgumentException if bound is not positive
     */
    int nextInt(int bound);
}
