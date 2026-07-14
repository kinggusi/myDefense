package com.denfense.server.service.balance;

import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.CommandLineRunner;
import org.springframework.core.annotation.Order;
import org.springframework.stereotype.Component;

@Slf4j
@Component
@RequiredArgsConstructor
@Order(5)
public class AlienSpecJsonSeedRunner implements CommandLineRunner {

    private final AlienSpecSeedService alienSpecSeedService;

    @Override
    public void run(String... args) throws Exception {
        AlienSpecSeedResult result = alienSpecSeedService.seed();

        if (!result.enabled()) {
            log.info("AlienSpec seed disabled");
        } else {
            log.info("AlienSpec seed completed: inserted={}, skipped={}",
                    result.insertedCount(), result.skippedCount());
        }
    }
}
