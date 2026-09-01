package com.denfense.server;

import com.denfense.server.domain.DailyContentType;
import com.denfense.server.dto.DailyContentDtos;
import com.denfense.server.repository.DailyContentProgressRepository;
import com.denfense.server.repository.DailyContentRunRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.DailyContentService;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.ActiveProfiles;

import java.util.List;
import java.util.concurrent.*;

import static org.junit.jupiter.api.Assertions.*;

@SpringBootTest
@ActiveProfiles("local")
class DailyContentConcurrencyIntegrationTest {
    @Autowired DailyContentService service;
    @Autowired UserRepository users;
    @Autowired DailyContentRunRepository runs;
    @Autowired DailyContentProgressRepository progresses;

    private String username;

    @BeforeEach
    void setUp() {
        username = "daily-concurrent-" + java.util.UUID.randomUUID();
        users.saveAndFlush(new com.denfense.server.domain.User(username, "pw"));
    }

    @AfterEach
    void tearDown() {
        runs.deleteAllInBatch();
        progresses.deleteAllInBatch();
        users.deleteAllInBatch();
    }

    @Test
    void sameEntryRequestInParallelConsumesOnceAndReturnsOneRetry() throws Exception {
        ExecutorService executor = Executors.newFixedThreadPool(2);
        CountDownLatch ready = new CountDownLatch(2);
        CountDownLatch start = new CountDownLatch(1);
        try {
            Callable<DailyContentDtos.RunResponse> task = () -> {
                ready.countDown();
                assertTrue(start.await(10, TimeUnit.SECONDS));
                return service.enter(new DailyContentDtos.EnterRequest(
                        "parallel-entry", username, DailyContentType.CULTIVATION_ZONE, 1));
            };
            Future<DailyContentDtos.RunResponse> first = executor.submit(task);
            Future<DailyContentDtos.RunResponse> second = executor.submit(task);
            assertTrue(ready.await(10, TimeUnit.SECONDS));
            start.countDown();

            List<DailyContentDtos.RunResponse> results = List.of(
                    first.get(15, TimeUnit.SECONDS), second.get(15, TimeUnit.SECONDS));
            assertEquals(1, results.stream().filter(DailyContentDtos.RunResponse::alreadyProcessed).count());
            assertEquals(1, runs.count());
            assertEquals(2, service.getProgress(username).contents().get(0).remainingEntries());
        } finally {
            executor.shutdownNow();
            assertTrue(executor.awaitTermination(10, TimeUnit.SECONDS));
        }
    }
}
