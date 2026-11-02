package com.denfense.server;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

import java.util.concurrent.atomic.AtomicBoolean;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.springframework.boot.ApplicationRunner;
import org.springframework.boot.WebApplicationType;
import org.springframework.boot.builder.SpringApplicationBuilder;
import org.springframework.boot.test.context.TestConfiguration;
import org.springframework.boot.test.system.CapturedOutput;
import org.springframework.boot.test.system.OutputCaptureExtension;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Import;

@ExtendWith(OutputCaptureExtension.class)
class LocalLauncherReadyReporterTest {
    private static final String MARKER = "458448ed-8739-4e72-bd3c-4591ea4c899e";

    @Test
    void signalAppearsOnlyAfterRunnersComplete(CapturedOutput output) {
        RunnerConfiguration.completed.set(false);
        try (var context = application(RunnerConfiguration.class).run()) {
            assertThat(RunnerConfiguration.completed).isTrue();
            assertThat(output.getOut()).contains("LOCAL_SERVER_APPLICATION_READY marker=" + MARKER);
        }
    }

    @Test
    void failedRunnerNeverSignalsReady(CapturedOutput output) {
        assertThatThrownBy(() -> application(FailedRunnerConfiguration.class).run()).isInstanceOf(IllegalStateException.class);
        assertThat(output.getOut()).doesNotContain("LOCAL_SERVER_APPLICATION_READY marker=");
    }

    @Test
    void productionMixedProfileDoesNotRegisterReporter(CapturedOutput output) {
        try (var context = application(RunnerConfiguration.class).profiles("local", "production").run()) {
            assertThat(context.getBeansOfType(LocalLauncherReadyReporter.class)).isEmpty();
            assertThat(output.getOut()).doesNotContain("LOCAL_SERVER_APPLICATION_READY marker=");
        }
    }

    private SpringApplicationBuilder application(Class<?> configuration) {
        return new SpringApplicationBuilder(configuration).web(WebApplicationType.NONE).profiles("local")
                .properties("spring.main.banner-mode=off", "spring.config.location=optional:classpath:launcher-no-config/",
                        "mydefense.local-launcher.ready-marker=" + MARKER);
    }

    @TestConfiguration(proxyBeanMethods = false)
    @Import(LocalLauncherReadyReporter.class)
    static class RunnerConfiguration {
        static final AtomicBoolean completed = new AtomicBoolean();
        @Bean ApplicationRunner databaseAndBalanceRunner() {
            return args -> completed.set(true);
        }
    }

    @TestConfiguration(proxyBeanMethods = false)
    @Import(LocalLauncherReadyReporter.class)
    static class FailedRunnerConfiguration {
        @Bean ApplicationRunner failedDatabaseRunner() {
            return args -> { throw new IllegalStateException("isolated runner fixture failure"); };
        }
    }
}
