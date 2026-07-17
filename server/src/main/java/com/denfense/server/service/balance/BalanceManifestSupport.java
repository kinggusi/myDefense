package com.denfense.server.service.balance;

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.List;
import java.util.Locale;
import java.util.Set;

public final class BalanceManifestSupport {

    public static final int SCHEMA_VERSION = 1;
    public static final String MANIFEST_FILE_NAME = "balance-manifest.json";
    public static final Set<String> REQUIRED_FILES = Set.of(
            "alien-level-stat.json",
            "alien-spec.json",
            "alien-upgrade-cost.json",
            "field-limit.json",
            "gacha-pools.json",
            "game-reward.json",
            "merge-rules.json",
            "monster-spec.json",
            "mythic-choice-balance.json",
            "mythic-breeding-config.json",
            "mythic-breeding-results.json",
            "shop-products.json",
            "summon-balance.json",
            "wave-spawn.json",
            "wave-spec.json"
    );

    private BalanceManifestSupport() {
    }

    public static String sha256(byte[] bytes) {
        try {
            return toHex(MessageDigest.getInstance("SHA-256").digest(bytes));
        } catch (NoSuchAlgorithmException e) {
            throw new IllegalStateException("SHA-256 is not available.", e);
        }
    }

    public static String contentHash(List<BalanceManifestFileEntry> entries) {
        StringBuilder canonical = new StringBuilder();
        for (BalanceManifestFileEntry entry : entries) {
            canonical.append(entry.name()).append('\0')
                    .append(entry.sha256()).append('\0')
                    .append(entry.size()).append('\n');
        }
        return sha256(canonical.toString().getBytes(StandardCharsets.UTF_8));
    }

    public static String balanceVersion(String contentHash) {
        return SCHEMA_VERSION + "-" + contentHash.substring(0, 16);
    }

    public static boolean isSafeFileName(String name) {
        return name != null
                && !MANIFEST_FILE_NAME.equals(name)
                && !name.contains("..")
                && name.matches("[A-Za-z0-9][A-Za-z0-9._-]*\\.json");
    }

    private static String toHex(byte[] bytes) {
        StringBuilder result = new StringBuilder(bytes.length * 2);
        for (byte value : bytes) {
            result.append(String.format(Locale.ROOT, "%02x", value & 0xff));
        }
        return result.toString();
    }
}
