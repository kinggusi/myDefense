package com.denfense.server.clan;

import org.springframework.stereotype.Component;
import java.time.Instant;

@Component
public class ClanClock { public Instant now() { return Instant.now(); } }
