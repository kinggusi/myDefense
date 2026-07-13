package com.denfense.server;

import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.service.balance.AlienSpecSeedResult;
import com.denfense.server.service.balance.AlienSpecSeedService;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.annotation.DirtiesContext;

import static org.assertj.core.api.Assertions.assertThat;

@SpringBootTest(properties = {
        "balance.alien-spec.seed-enabled=false",
        "spring.datasource.url=jdbc:h2:mem:testdb_seed_disabled;MODE=MySQL"
})
@DirtiesContext(classMode = DirtiesContext.ClassMode.AFTER_CLASS)
public class AlienSpecSeedDisabledIntegrationTest {

    @Autowired
    private AlienSpecRepository alienSpecRepository;

    @Autowired
    private AlienSpecSeedService seedService;

    @Test
    @DisplayName("seed-enabled=false → Seed 미실행, AlienSpec 0건")
    void seedDisabled_noAlienSpecs() {
        // Seed Runner가 실행되더라도 SeedService에서 즉시 반환
        assertThat(alienSpecRepository.count()).isZero();

        AlienSpecSeedResult result = seedService.seed();
        assertThat(result.enabled()).isFalse();
        assertThat(result.insertedCount()).isZero();
        assertThat(result.skippedCount()).isZero();
    }
}
