package com.denfense.server.auth;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.CommandLineRunner;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.context.annotation.Profile;
import org.springframework.core.annotation.Order;
import org.springframework.stereotype.Component;
import java.util.Arrays;

@Component @Order(20)
@Profile("(local | dev) & !prod & !production")
@ConditionalOnProperty(name = {"mydefense.auth.local-accounts.enabled", "mydefense.auth.local-accounts.bootstrap-enabled"}, havingValue = "true")
public class LocalDeveloperAccountBootstrap implements CommandLineRunner {
    private final LocalDeveloperAccountService service;
    private final String usernames;
    private final String password;
    public LocalDeveloperAccountBootstrap(LocalDeveloperAccountService service,
            @Value("${mydefense.auth.local-accounts.usernames:}") String usernames,
            @Value("${mydefense.auth.local-accounts.password:}") String password) {
        this.service = service; this.usernames = usernames; this.password = password;
    }
    @Override public void run(String... args) {
        service.provision(Arrays.stream(usernames.split(",")).map(String::trim).toList(), password);
    }
}
