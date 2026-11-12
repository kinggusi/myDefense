package com.denfense.server.auth;

import com.denfense.server.DefenseServerApplication;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.shop.ShopRandomSource;
import org.springframework.boot.WebApplicationType;
import org.springframework.boot.builder.SpringApplicationBuilder;
import org.springframework.boot.test.context.TestConfiguration;
import org.springframework.context.ConfigurableApplicationContext;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Primary;
import org.springframework.transaction.PlatformTransactionManager;
import org.springframework.transaction.support.TransactionTemplate;

import java.io.IOException;
import java.nio.channels.FileChannel;
import java.nio.channels.FileLock;
import java.nio.file.Files;
import java.nio.file.LinkOption;
import java.nio.file.Path;
import java.nio.file.StandardOpenOption;
import java.security.SecureRandom;
import java.util.Base64;
import java.util.UUID;

/** Explicit test-classpath-only process-restart harness. No production endpoint or data source. */
public final class ShopPersistenceUiServer {
    static final String GUEST_SECRET = "E".repeat(42) + "A";
    static final int INITIAL_GOLD = 5_000_000;
    static final int INITIAL_DIAMOND = 100_000;
    private static final String MARKER_PREFIX = "SHOP_UI_PERSISTENCE_V1\n";

    public static void main(String[] args) throws Exception {
        if (args.length != 3 || !"--shop-persistence-ui".equals(args[0]))
            throw new IllegalArgumentException("Explicit --shop-persistence-ui fresh|resume canonical-UUID required");
        PreparedRun run = prepare(args[1], args[2]);
        try {
            var context = start(run, WebApplicationType.SERVLET);
            Runtime.getRuntime().addShutdownHook(new Thread(() -> {
                try { context.close(); } finally { run.close(); }
            }, "shop-persistence-ui-cleanup"));
            System.out.println("SHOP_PERSISTENCE_UI_READY loopback=18085 mode=" + args[1]
                    + " runId=" + args[2] + " testOnly=true forcedLucky=true");
        } catch (Exception failure) { run.close(); throw failure; }
    }

    static Path root() throws IOException {
        Path project = Path.of("").toRealPath();
        if (!Files.isRegularFile(project.resolve("build.gradle")) || !Files.isDirectory(project.resolve("src/test/java")))
            throw new IllegalArgumentException("Run this test harness from the server project only");
        Path root = project.resolve("build/shop-ui-persistence");
        rejectRedirectedExisting(project.resolve("build"));
        rejectRedirectedExisting(root);
        Files.createDirectories(root);
        rejectRedirectedExisting(root);
        return root;
    }

    static PreparedRun prepare(String mode, String runId) throws IOException {
        if (!"fresh".equals(mode) && !"resume".equals(mode)) throw new IllegalArgumentException("fresh or resume required");
        if (runId == null || !UUID.fromString(runId).toString().equals(runId))
            throw new IllegalArgumentException("Canonical UUID required");
        Path directory = root().resolve(runId).normalize();
        boolean fresh = "fresh".equals(mode);
        if (fresh) Files.createDirectory(directory); // Existing or incomplete runs are never reset/adopted.
        else if (!Files.isDirectory(directory, LinkOption.NOFOLLOW_LINKS)) throw new IllegalStateException("Resume run is missing");
        rejectRedirectedExisting(directory);
        Path keyFile = directory.resolve("signing-key.test-only");
        Path marker = directory.resolve("ready.marker");
        Path database = directory.resolve("shop.mv.db");
        rejectRedirectedExisting(directory.resolve("process.lock"));
        FileChannel channel = FileChannel.open(directory.resolve("process.lock"), StandardOpenOption.CREATE, StandardOpenOption.WRITE);
        FileLock lease = null;
        try {
            lease = channel.tryLock();
            if (lease == null) throw new IllegalStateException("The test run is already in use");
            if (fresh) {
                byte[] key = new byte[32]; new SecureRandom().nextBytes(key);
                Files.writeString(keyFile, Base64.getEncoder().encodeToString(key), StandardOpenOption.CREATE_NEW);
                java.util.Arrays.fill(key, (byte)0);
            } else {
                requireRegularFile(marker); requireRegularFile(database); requireRegularFile(keyFile);
                if (Files.size(marker) > 100 || !Files.readString(marker).equals(MARKER_PREFIX + runId))
                    throw new IllegalStateException("Run marker does not match; resume refused");
            }
            requireRegularFile(keyFile);
            if (Files.size(keyFile) != 44) throw new IllegalStateException("Invalid test signing key envelope");
            String key = Files.readString(keyFile);
            try { if (Base64.getDecoder().decode(key).length != 32) throw new IllegalArgumentException(); }
            catch (IllegalArgumentException invalid) { throw new IllegalStateException("Invalid test signing key envelope"); }
            return new PreparedRun(directory, runId, fresh, key, channel, lease);
        } catch (Exception failure) {
            if (lease != null) lease.close(); channel.close(); throw failure;
        }
    }

    private static void requireRegularFile(Path path) throws IOException {
        if (!Files.isRegularFile(path, LinkOption.NOFOLLOW_LINKS)) throw new IllegalStateException("Required run file is missing or redirected");
        rejectRedirectedExisting(path);
    }
    private static void rejectRedirectedExisting(Path path) throws IOException {
        if (Files.exists(path, LinkOption.NOFOLLOW_LINKS)
                && (Files.isSymbolicLink(path) || !path.toRealPath().equals(path.toAbsolutePath().normalize())))
            throw new IllegalStateException("Redirected test paths are not allowed");
    }

    static ConfigurableApplicationContext start(PreparedRun run, WebApplicationType web) throws IOException {
        if (!run.started.compareAndSet(false, true)) throw new IllegalStateException("Prepared run can only start once");
        if (run.fresh && (Files.exists(run.directory.resolve("shop.mv.db")) || Files.exists(run.directory.resolve("ready.marker"))))
            throw new IllegalStateException("Fresh run acquired existing state; startup refused");
        String database = run.directory.resolve("shop").toString().replace('\\', '/');
        if (database.contains(";")) throw new IllegalStateException("Unsupported test root");
        var context = new SpringApplicationBuilder(DefenseServerApplication.class, DeterministicShop.class).web(web)
                .profiles("local").run("--spring.profiles.active=local", "--server.address=127.0.0.1", "--server.port=18085",
                        "--spring.datasource.url=jdbc:h2:file:" + database + ";MODE=MySQL",
                        "--spring.jpa.hibernate.ddl-auto=update", "--spring.jpa.show-sql=false", "--spring.sql.init.mode=never",
                        "--mydefense.auth.signing-key-base64=" + run.signingKey,
                        "--mydefense.auth.local-accounts.enabled=false", "--mydefense.auth.local-accounts.bootstrap-enabled=false",
                        "--mydefense.collection.tutorial-mythic-preview-enabled=false",
                        "--mydefense.battle.allow-anonymous-entry-snapshots=false", "--mydefense.shop.development-ads-enabled=true",
                        "--logging.level.com.zaxxer.hikari=WARN", "--logging.level.org.hibernate=WARN");
        try {
            if (run.fresh) {
                long id = context.getBean(GuestAccountWriter.class).findOrCreate(AuthSecrets.hash(GUEST_SECRET));
                var tx = new TransactionTemplate(context.getBean(PlatformTransactionManager.class));
                tx.executeWithoutResult(status -> {
                    var user = context.getBean(UserRepository.class).findByIdForUpdate(id).orElseThrow();
                    user.setAccountLevel(10); user.setGold(INITIAL_GOLD); user.setDiamond(INITIAL_DIAMOND);
                });
                Files.writeString(run.directory.resolve("ready.marker"), MARKER_PREFIX + run.runId, StandardOpenOption.CREATE_NEW);
            } else if (context.getBean(GuestCredentialRepository.class).findById(AuthSecrets.hash(GUEST_SECRET)).isEmpty()) {
                throw new IllegalStateException("Fixture account missing; resume never reseeds");
            }
            return context;
        } catch (Exception failure) { context.close(); throw failure; }
    }

    @TestConfiguration static class DeterministicShop {
        @Bean @Primary ShopRandomSource deterministicShopRandom() {
            return new ShopRandomSource() {
                @Override public int nextInt(int bound) {
                    if (bound <= 0) throw new IllegalArgumentException("Positive bound required");
                    return 0; // Force the lucky branch and canonical first shop choice only. Gacha source is untouched.
                }
            };
        }
    }

    static final class PreparedRun implements AutoCloseable {
        final Path directory;
        final String runId;
        final boolean fresh;
        private final String signingKey;
        private final FileChannel channel;
        private final FileLock lease;
        private final java.util.concurrent.atomic.AtomicBoolean started = new java.util.concurrent.atomic.AtomicBoolean();
        PreparedRun(Path directory, String runId, boolean fresh, String signingKey, FileChannel channel, FileLock lease) {
            this.directory=directory; this.runId=runId; this.fresh=fresh; this.signingKey=signingKey; this.channel=channel; this.lease=lease;
        }
        @Override public void close() {
            try { if (lease.isValid()) lease.release(); channel.close(); }
            catch (IOException failure) { throw new IllegalStateException("Could not release isolated test run", failure); }
        }
    }
}
