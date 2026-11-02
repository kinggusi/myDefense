package com.denfense.server.auth;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;
import org.springframework.boot.test.context.runner.ApplicationContextRunner;
import static org.assertj.core.api.Assertions.assertThat;

class LocalDeveloperProfileGuardTest {
    private final ApplicationContextRunner runner = new ApplicationContextRunner()
            .withUserConfiguration(LocalDeveloperAccountBootstrap.class,
                    LocalDeveloperAccountService.class, LocalDeveloperLoginController.class);

    @Test void localAccountsAreOffByDefault() {
        runner.withPropertyValues("spring.profiles.active=local").run(this::assertAbsent);
    }

    @ParameterizedTest @ValueSource(strings = {"prod", "production", "local,prod", "local,production", "dev,production"})
    void deployedProfilesCannotEnableLocalAccountsEvenWhenFlagsAreSet(String profiles) {
        runner.withPropertyValues("spring.profiles.active=" + profiles,
                "mydefense.auth.local-accounts.enabled=true",
                "mydefense.auth.local-accounts.bootstrap-enabled=true").run(this::assertAbsent);
    }

    @Test void noProfileCannotEnableLocalAccounts() {
        runner.withPropertyValues("mydefense.auth.local-accounts.enabled=true",
                "mydefense.auth.local-accounts.bootstrap-enabled=true").run(this::assertAbsent);
    }

    private void assertAbsent(org.springframework.boot.test.context.assertj.AssertableApplicationContext context) {
        assertThat(context).hasNotFailed();
        assertThat(context).doesNotHaveBean(LocalDeveloperAccountBootstrap.class);
        assertThat(context).doesNotHaveBean(LocalDeveloperAccountService.class);
        assertThat(context).doesNotHaveBean(LocalDeveloperLoginController.class);
    }
}
