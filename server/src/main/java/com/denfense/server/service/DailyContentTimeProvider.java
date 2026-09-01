package com.denfense.server.service;

import org.springframework.stereotype.Component;

import java.time.LocalDate;
import java.time.ZoneId;

@Component
public class DailyContentTimeProvider {
    public static final ZoneId RESET_ZONE = ZoneId.of("Asia/Seoul");

    public LocalDate today() {
        return LocalDate.now(RESET_ZONE);
    }
}
