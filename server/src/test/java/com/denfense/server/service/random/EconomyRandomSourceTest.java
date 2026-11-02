package com.denfense.server.service.random;

import com.denfense.server.service.gacha.DefaultGachaRandomGenerator;
import org.junit.jupiter.api.Test;

import static org.assertj.core.api.Assertions.*;
import static org.mockito.Mockito.*;

class EconomyRandomSourceTest {
    @Test void rejectsNonPositiveBoundsAndKeepsUpperBoundExclusive() {
        var random = new EconomyRandomSource();
        assertThatThrownBy(() -> random.nextInt(0)).isInstanceOf(IllegalArgumentException.class);
        assertThatThrownBy(() -> random.nextInt(-1)).isInstanceOf(IllegalArgumentException.class);
        assertThat(random.nextInt(1)).isZero();
        for (int bound : new int[]{2, 7, 10000, Integer.MAX_VALUE})
            for (int i = 0; i < 100; i++) assertThat(random.nextInt(bound)).isBetween(0, bound - 1);
    }

    @Test void gachaAdapterPreservesTheExistingInjectableContract() {
        var random = mock(EconomyRandomSource.class);
        when(random.nextInt(10000)).thenReturn(9999);
        var gacha = new DefaultGachaRandomGenerator(random);
        assertThat(gacha.nextInt(10000)).isEqualTo(9999);
        verify(random).nextInt(10000);
        assertThatThrownBy(() -> gacha.nextInt(0)).isInstanceOf(IllegalArgumentException.class);
        verifyNoMoreInteractions(random);
    }
}
