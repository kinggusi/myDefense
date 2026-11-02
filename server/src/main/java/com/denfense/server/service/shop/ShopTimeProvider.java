package com.denfense.server.service.shop;
import org.springframework.stereotype.Component;
import java.time.Instant;
@Component public class ShopTimeProvider { public Instant now() { return Instant.now(); } }
