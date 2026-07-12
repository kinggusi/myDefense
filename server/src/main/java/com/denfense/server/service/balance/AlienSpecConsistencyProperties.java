package com.denfense.server.service.balance;

import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.stereotype.Component;

@Component
@ConfigurationProperties(prefix = "balance.alien-spec")
public class AlienSpecConsistencyProperties {

    private AlienSpecConsistencyMode consistencyMode = AlienSpecConsistencyMode.WARN;

    public AlienSpecConsistencyMode getConsistencyMode() {
        return consistencyMode;
    }

    public void setConsistencyMode(AlienSpecConsistencyMode consistencyMode) {
        this.consistencyMode = consistencyMode;
    }
}
