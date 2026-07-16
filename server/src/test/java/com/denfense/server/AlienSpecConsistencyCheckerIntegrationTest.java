package com.denfense.server;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.service.balance.AlienSpecConsistencyChecker;
import com.denfense.server.service.balance.AlienSpecConsistencyException;
import com.denfense.server.service.balance.AlienSpecConsistencyMode;
import com.denfense.server.service.balance.AlienSpecConsistencyProperties;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;

import static org.assertj.core.api.Assertions.assertThat;
import static org.junit.jupiter.api.Assertions.assertThrows;

@SpringBootTest
public class AlienSpecConsistencyCheckerIntegrationTest {

    @Autowired
    private AlienSpecConsistencyChecker checker;

    @Autowired
    private AlienSpecRepository alienSpecRepository;

    @Autowired
    private AlienSpecConsistencyProperties properties;

    private AlienSpecConsistencyMode originalMode;

    @BeforeEach
    void setUp() {
        originalMode = properties.getConsistencyMode();
    }

    @AfterEach
    void tearDown() {
        properties.setConsistencyMode(originalMode);
    }

    @Test
    void testDataInitCompletionAndCheckerExecution() throws Exception {
        // DataInit already populated the DB and checker ran during context start
        // We will just verify the current DB state vs registry
        long count = alienSpecRepository.count();
        assertThat(count).isEqualTo(48);

        // Run checker explicitly with FAIL to ensure everything matches
        properties.setConsistencyMode(AlienSpecConsistencyMode.FAIL);
        checker.run(); // Should not throw
    }

    @Test
    void testFailModeThrowsOnMismatch() {
        properties.setConsistencyMode(AlienSpecConsistencyMode.FAIL);

        AlienSpec original = alienSpecRepository.findById(1L).orElseThrow();
        int oldAtk = original.getBaseAtk();

        try {
            original.setBaseAtk(9999);
            alienSpecRepository.save(original); // update

            AlienSpecConsistencyException ex = assertThrows(AlienSpecConsistencyException.class, () -> checker.run());
            assertThat(ex.getMessage()).contains("alienId=1");
            assertThat(ex.getMessage()).contains("field=baseAtk");
        } finally {
            original.setBaseAtk(oldAtk);
            alienSpecRepository.save(original); // restore
        }
    }

    @Test
    void testWarnModeDoesNotThrowOnMismatch() throws Exception {
        properties.setConsistencyMode(AlienSpecConsistencyMode.WARN);

        AlienSpec original = alienSpecRepository.findById(2L).orElseThrow();
        int oldAtk = original.getBaseAtk();

        try {
            original.setBaseAtk(9999);
            alienSpecRepository.save(original); // update

            // Should not throw, only logs warn
            checker.run();
        } finally {
            original.setBaseAtk(oldAtk);
            alienSpecRepository.save(original); // restore
        }
    }

    @Test
    void testReadOnlyNoDbModification() throws Exception {
        properties.setConsistencyMode(AlienSpecConsistencyMode.WARN);

        long beforeCount = alienSpecRepository.count();
        checker.run();
        long afterCount = alienSpecRepository.count();

        assertThat(afterCount).isEqualTo(beforeCount);
    }
}
