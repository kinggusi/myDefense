package com.denfense.server.auth;

import com.denfense.server.dto.shop.ShopDtos.*;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.shop.ShopCatalogService;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;
import org.springframework.boot.WebApplicationType;
import org.springframework.jdbc.core.JdbcTemplate;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.UUID;
import static org.assertj.core.api.Assertions.*;

class ShopPersistenceUiServerTest {
    private final List<Path> ownedRuns = new ArrayList<>();
    private String newRunId() throws Exception {
        String id = UUID.randomUUID().toString(); ownedRuns.add(ShopPersistenceUiServer.root().resolve(id)); return id;
    }

    @AfterEach void cleanOwnedTestRuns() throws Exception {
        Path root = ShopPersistenceUiServer.root().toRealPath();
        for (Path run : ownedRuns) {
            if (!Files.exists(run)) continue;
            Path actual = run.toRealPath();
            if (!actual.getParent().equals(root) || !actual.getFileName().toString().equals(UUID.fromString(actual.getFileName().toString()).toString()))
                throw new IllegalStateException("Refusing test cleanup outside the owned UUID directory");
            try (var entries = Files.walk(actual)) {
                for (Path entry : entries.sorted(Comparator.reverseOrder()).toList()) Files.delete(entry);
            }
        }
    }

    @ParameterizedTest @ValueSource(strings = {"../outside", "", "00000000-0000-0000-0000-00000000000Z", "1-1-1-1-1"})
    void onlyCanonicalUuidMayChooseRunDirectory(String id) {
        assertThatThrownBy(() -> ShopPersistenceUiServer.prepare("fresh", id)).isInstanceOf(IllegalArgumentException.class);
    }

    @Test void freshNeverAdoptsOrResetsExistingRunAndIncompleteRunCannotResume() throws Exception {
        String id = newRunId(); byte[] originalKey;
        try (var run = ShopPersistenceUiServer.prepare("fresh", id)) {
            originalKey = Files.readAllBytes(run.directory.resolve("signing-key.test-only"));
            assertThatThrownBy(() -> ShopPersistenceUiServer.prepare("fresh", id)).isInstanceOf(java.nio.file.FileAlreadyExistsException.class);
        }
        assertThatThrownBy(() -> ShopPersistenceUiServer.prepare("resume", id)).isInstanceOf(IllegalStateException.class);
        assertThat(java.util.Arrays.equals(Files.readAllBytes(ShopPersistenceUiServer.root().resolve(id).resolve("signing-key.test-only")), originalKey)).isTrue();
    }

    @Test void modeAndMissingResumeAreRejected() throws Exception {
        String id = newRunId();
        assertThatThrownBy(() -> ShopPersistenceUiServer.prepare("reset", id)).isInstanceOf(IllegalArgumentException.class);
        assertThatThrownBy(() -> ShopPersistenceUiServer.prepare("resume", id)).isInstanceOf(IllegalStateException.class);
    }

    @Test void freshRunCannotAdoptDatabaseAddedAfterPreparation() throws Exception {
        try (var run = ShopPersistenceUiServer.prepare("fresh", newRunId())) {
            Files.writeString(run.directory.resolve("shop.mv.db"), "unowned-state");
            assertThatThrownBy(() -> ShopPersistenceUiServer.start(run, WebApplicationType.NONE))
                    .isInstanceOf(IllegalStateException.class).hasMessageContaining("Fresh run acquired existing state");
            assertThat(Files.readString(run.directory.resolve("shop.mv.db"))).isEqualTo("unowned-state");
        }
    }

    @Test void distinctRunsDoNotShareKeysOrDatabaseLocation() throws Exception {
        try (var first = ShopPersistenceUiServer.prepare("fresh", newRunId());
             var second = ShopPersistenceUiServer.prepare("fresh", newRunId())) {
            assertThat(first.directory.equals(second.directory)).isFalse();
            assertThat(java.util.Arrays.equals(Files.readAllBytes(first.directory.resolve("signing-key.test-only")),
                    Files.readAllBytes(second.directory.resolve("signing-key.test-only")))).isFalse();
        }
    }

    @Test void resumeRequiresMatchingMarkerValidKeyAndExclusiveLease() throws Exception {
        String id = newRunId(); Path directory;
        try (var run = ShopPersistenceUiServer.prepare("fresh", id)) {
            directory = run.directory;
            Files.writeString(directory.resolve("shop.mv.db"), "unit-test-envelope-only");
            Files.writeString(directory.resolve("ready.marker"), "SHOP_UI_PERSISTENCE_V1\n" + id);
            assertThatThrownBy(() -> ShopPersistenceUiServer.prepare("resume", id)).isInstanceOf(java.nio.channels.OverlappingFileLockException.class);
        }
        Files.writeString(directory.resolve("ready.marker"), "SHOP_UI_PERSISTENCE_V1\n" + UUID.randomUUID());
        assertThatThrownBy(() -> ShopPersistenceUiServer.prepare("resume", id)).isInstanceOf(IllegalStateException.class);
        Files.writeString(directory.resolve("ready.marker"), "SHOP_UI_PERSISTENCE_V1\n" + id);
        Files.writeString(directory.resolve("signing-key.test-only"), "invalid");
        assertThatThrownBy(() -> ShopPersistenceUiServer.prepare("resume", id)).isInstanceOf(IllegalStateException.class);
    }

    @Test void fileContextRestartPreservesAuthWalletLuckyAdsReceiptAndNeverReseeds() throws Exception {
        String id = newRunId(); long userId; AuthDtos.SessionResponse tokens; Catalog before;
        PurchaseRequest purchase; PurchaseResponse original; long usersBefore;
        try (var run = ShopPersistenceUiServer.prepare("fresh", id);
             var context = ShopPersistenceUiServer.start(run, WebApplicationType.NONE)) {
            assertThat(context.getEnvironment().getProperty("spring.jpa.hibernate.ddl-auto")).isEqualTo("update");
            assertThat(context.getEnvironment().getProperty("spring.sql.init.mode")).isEqualTo("never");
            assertThat(context.getEnvironment().getProperty("mydefense.auth.local-accounts.enabled")).isEqualTo("false");
            var auth = context.getBean(AuthService.class); tokens = auth.guest(ShopPersistenceUiServer.GUEST_SECRET); userId = tokens.user().userId();
            var shop = context.getBean(ShopCatalogService.class); Catalog initial = shop.catalog(userId);
            assertThat(initial.wallet().gold()).isEqualTo(ShopPersistenceUiServer.INITIAL_GOLD);
            assertThat(initial.wallet().diamond()).isEqualTo(ShopPersistenceUiServer.INITIAL_DIAMOND);
            assertThat(initial.offers()).anyMatch(o -> "LUCKY".equals(o.category()));
            assertThat(initial.developmentAd()).isTrue(); assertThat(initial.rerollsRemaining()).isEqualTo(3);
            Offer dna = initial.offers().stream().filter(o -> "DNA_PACK".equals(o.offerId())).findFirst().orElseThrow();
            purchase = new PurchaseRequest(UUID.randomUUID(), dna.offerId(), dna.slotKey(), initial.catalogRevision(), dna.price(), dna.currencyType());
            original = shop.purchase(userId, purchase);
            var ad = shop.challenge(userId, UUID.randomUUID()); shop.completeDevelopmentAd(userId, ad.challengeId());
            shop.reroll(userId, new RerollRequest(UUID.randomUUID(), ad.challengeId(), shop.catalog(userId).catalogRevision()));
            before = shop.catalog(userId); usersBefore = context.getBean(UserRepository.class).count();
            assertThatThrownBy(() -> ShopPersistenceUiServer.start(run, WebApplicationType.NONE))
                    .isInstanceOf(IllegalStateException.class).hasMessageContaining("only start once");
        }
        try (var run = ShopPersistenceUiServer.prepare("resume", id);
             var context = ShopPersistenceUiServer.start(run, WebApplicationType.NONE)) {
            var auth = context.getBean(AuthService.class);
            assertThat(auth.authenticate(tokens.accessToken()).userId()).isEqualTo(userId);
            assertThat(auth.refresh(tokens.refreshToken()).user().userId()).isEqualTo(userId);
            assertThat(context.getBean(UserRepository.class).count()).isEqualTo(usersBefore);
            var shop = context.getBean(ShopCatalogService.class); Catalog after = shop.catalog(userId);
            assertThat(after.wallet()).isEqualTo(before.wallet()); assertThat(after.offers()).isEqualTo(before.offers());
            assertThat(after.catalogRevision()).isEqualTo(before.catalogRevision()); assertThat(after.rerollsRemaining()).isEqualTo(2);
            assertThat(shop.purchase(userId, purchase)).isEqualTo(original);
            assertThat(shop.catalog(userId).wallet()).isEqualTo(before.wallet());
            // A damaged resume must not recreate the fixture account or fund an arbitrary account.
            context.getBean(JdbcTemplate.class).update("delete from auth_guest_credentials where secret_hash=?", AuthSecrets.hash(ShopPersistenceUiServer.GUEST_SECRET));
        }
        try (var run = ShopPersistenceUiServer.prepare("resume", id)) {
            assertThatThrownBy(() -> ShopPersistenceUiServer.start(run, WebApplicationType.NONE))
                    .isInstanceOf(IllegalStateException.class).hasMessageContaining("resume never reseeds");
        }
    }
}
