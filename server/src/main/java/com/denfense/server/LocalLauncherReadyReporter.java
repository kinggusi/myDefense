package com.denfense.server;

import java.util.UUID;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.boot.context.event.ApplicationReadyEvent;
import org.springframework.context.annotation.Profile;
import org.springframework.context.event.EventListener;
import org.springframework.core.Ordered;
import org.springframework.core.annotation.Order;
import org.springframework.stereotype.Component;

/** Local launcher handshake only: ApplicationReady follows every database/balance runner. */
@Slf4j
@Component
@Profile("local & !prod & !production")
@ConditionalOnProperty(name = "mydefense.local-launcher.ready-marker")
public class LocalLauncherReadyReporter {
    private final UUID marker;

    public LocalLauncherReadyReporter(@Value("${mydefense.local-launcher.ready-marker}") String marker) {
        this.marker = UUID.fromString(marker);
    }

    @EventListener(ApplicationReadyEvent.class)
    @Order(Ordered.LOWEST_PRECEDENCE)
    public void ready() {
        log.info("LOCAL_SERVER_APPLICATION_READY marker={}", marker);
    }
}
