package com.denfense.server.balance.tool.battle;

import com.denfense.server.balance.tool.BalanceConversionException;

import java.io.IOException;
import java.math.BigDecimal;
import java.nio.charset.StandardCharsets;
import java.nio.file.AtomicMoveNotSupportedException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.function.Function;

import static com.denfense.server.balance.tool.battle.BattleBalanceData.*;

public final class BattleBalanceJsonWriter {
    public static final int SCHEMA_VERSION = 1;
    public static final String BALANCE_VERSION = "battle-v1";

    public static final List<FileDefinition<?>> FILES = List.of(
            new FileDefinition<>("Balance/Battle/wave-spec", "wave-spec.json", Data::waves,
                    Comparator.comparingInt(WaveSpec::roundNumber).thenComparing(WaveSpec::waveId), BattleBalanceJsonWriter::waveJson),
            new FileDefinition<>("Balance/Battle/wave-spawn-spec", "wave-spawn-spec.json", Data::spawns,
                    Comparator.comparing(WaveSpawnSpec::waveId).thenComparingInt(WaveSpawnSpec::spawnOrder), BattleBalanceJsonWriter::spawnJson),
            new FileDefinition<>("Balance/Battle/boss-pattern-spec", "boss-pattern-spec.json", Data::bossPatterns,
                    Comparator.comparing(BossPatternSpec::waveId).thenComparingInt(BossPatternSpec::patternOrder), BattleBalanceJsonWriter::bossPatternJson),
            new FileDefinition<>("Balance/Battle/skill-spec", "skill-spec.json", Data::skills,
                    Comparator.comparing(SkillSpec::skillId), BattleBalanceJsonWriter::skillJson),
            new FileDefinition<>("Balance/Battle/alien-skill-links", "alien-skill-links.json", Data::alienSkillLinks,
                    Comparator.comparingLong(AlienSkillLink::alienId).thenComparingInt(AlienSkillLink::slotIndex), BattleBalanceJsonWriter::alienSkillJson),
            new FileDefinition<>("Balance/Battle/projectile-spec", "projectile-spec.json", Data::projectiles,
                    Comparator.comparing(ProjectileSpec::projectileId), BattleBalanceJsonWriter::projectileJson),
            new FileDefinition<>("Balance/Battle/skill-effect-spec", "skill-effect-spec.json", Data::skillEffects,
                    Comparator.comparing(SkillEffectSpec::skillId).thenComparingInt(SkillEffectSpec::executionOrder), BattleBalanceJsonWriter::skillEffectJson));

    public WriteResult writeAll(Data data, Path outputDirectory) {
        Map<String, DocumentOutput> documents = new LinkedHashMap<>();
        for (FileDefinition<?> rawDefinition : FILES) writeDocument(data, outputDirectory, rawDefinition, documents);

        List<DocumentOutput> sorted = new ArrayList<>(documents.values());
        sorted.sort(Comparator.comparing(DocumentOutput::resourcePath));
        StringBuilder bundlePayload = new StringBuilder();
        for (DocumentOutput document : sorted)
            bundlePayload.append(document.resourcePath()).append(':').append(document.contentHash()).append('\n');
        String bundleHash = sha256(bundlePayload.toString());
        String manifestJson = manifestJson(sorted, bundleHash);
        writeUtf8Atomic(outputDirectory.resolve("battle-balance-manifest.json"), manifestJson);
        return new WriteResult(Map.copyOf(documents), bundleHash, manifestJson);
    }

    @SuppressWarnings("unchecked")
    private static <T> void writeDocument(
            Data data,
            Path outputDirectory,
            FileDefinition<?> rawDefinition,
            Map<String, DocumentOutput> output) {
        FileDefinition<T> definition = (FileDefinition<T>) rawDefinition;
        List<T> items = new ArrayList<>(definition.selector().apply(data));
        items.sort(definition.comparator());
        List<String> itemJson = items.stream().map(definition.serializer()).toList();
        String canonicalPayload = canonicalPayload(itemJson);
        String contentHash = sha256(canonicalPayload);
        String documentJson = documentJson(itemJson, contentHash);
        writeUtf8Atomic(outputDirectory.resolve(definition.fileName()), documentJson);
        output.put(definition.resourcePath(), new DocumentOutput(
                definition.resourcePath(), definition.fileName(), contentHash, canonicalPayload, documentJson));
    }

    private static String canonicalPayload(List<String> items) {
        return "{\"schemaVersion\":" + SCHEMA_VERSION
                + ",\"balanceVersion\":" + quote(BALANCE_VERSION)
                + ",\"items\":[" + String.join(",", items) + "]}";
    }

    private static String documentJson(List<String> items, String contentHash) {
        StringBuilder json = new StringBuilder();
        json.append("{\n")
                .append("  \"schemaVersion\": ").append(SCHEMA_VERSION).append(",\n")
                .append("  \"balanceVersion\": ").append(quote(BALANCE_VERSION)).append(",\n")
                .append("  \"contentHash\": ").append(quote(contentHash)).append(",\n")
                .append("  \"items\": [");
        if (!items.isEmpty()) {
            json.append('\n');
            for (int index = 0; index < items.size(); index++) {
                json.append("    ").append(items.get(index));
                if (index + 1 < items.size()) json.append(',');
                json.append('\n');
            }
            json.append("  ");
        }
        return json.append("]\n}\n").toString();
    }

    private static String manifestJson(List<DocumentOutput> documents, String bundleHash) {
        StringBuilder json = new StringBuilder();
        json.append("{\n")
                .append("  \"schemaVersion\": ").append(SCHEMA_VERSION).append(",\n")
                .append("  \"balanceVersion\": ").append(quote(BALANCE_VERSION)).append(",\n")
                .append("  \"bundleHash\": ").append(quote(bundleHash)).append(",\n")
                .append("  \"files\": [\n");
        for (int index = 0; index < documents.size(); index++) {
            DocumentOutput document = documents.get(index);
            json.append("    {\"resourcePath\":").append(quote(document.resourcePath()))
                    .append(",\"contentHash\":").append(quote(document.contentHash())).append('}');
            if (index + 1 < documents.size()) json.append(',');
            json.append('\n');
        }
        return json.append("  ]\n}\n").toString();
    }

    private static String waveJson(WaveSpec value) {
        return "{\"waveId\":" + quote(value.waveId())
                + ",\"roundNumber\":" + value.roundNumber()
                + ",\"waveType\":" + quote(value.waveType())
                + ",\"nextWaveDelaySeconds\":" + number(value.nextWaveDelaySeconds())
                + ",\"bossTimeLimitSeconds\":" + number(value.bossTimeLimitSeconds())
                + ",\"enabled\":" + value.enabled() + "}";
    }

    private static String spawnJson(WaveSpawnSpec value) {
        return "{\"waveId\":" + quote(value.waveId())
                + ",\"spawnOrder\":" + value.spawnOrder()
                + ",\"lanePolicy\":" + quote(value.lanePolicy())
                + ",\"monsterId\":" + quote(value.monsterId())
                + ",\"spawnCount\":" + value.spawnCount()
                + ",\"spawnDelaySeconds\":" + number(value.spawnDelaySeconds())
                + ",\"spawnIntervalSeconds\":" + number(value.spawnIntervalSeconds())
                + ",\"hpMultiplier\":" + number(value.hpMultiplier())
                + ",\"moveSpeedMultiplier\":" + number(value.moveSpeedMultiplier()) + "}";
    }

    private static String bossPatternJson(BossPatternSpec value) {
        return "{\"waveId\":" + quote(value.waveId())
                + ",\"patternOrder\":" + value.patternOrder()
                + ",\"patternType\":" + quote(value.patternType())
                + ",\"triggerType\":" + quote(value.triggerType())
                + ",\"triggerValue\":" + number(value.triggerValue())
                + ",\"cooldownSeconds\":" + number(value.cooldownSeconds())
                + ",\"skillId\":" + quote(value.skillId())
                + ",\"parameterKey\":" + quote(value.parameterKey())
                + ",\"parameterValue\":" + number(value.parameterValue())
                + ",\"enabled\":" + value.enabled() + "}";
    }

    private static String skillJson(SkillSpec value) {
        return "{\"skillId\":" + quote(value.skillId())
                + ",\"nameKey\":" + quote(value.nameKey())
                + ",\"descriptionKey\":" + quote(value.descriptionKey())
                + ",\"skillType\":" + quote(value.skillType())
                + ",\"triggerType\":" + quote(value.triggerType())
                + ",\"cooldownSeconds\":" + number(value.cooldownSeconds())
                + ",\"mpCost\":" + number(value.mpCost())
                + ",\"castRange\":" + number(value.castRange())
                + ",\"targetPolicy\":" + quote(value.targetPolicy())
                + ",\"maxTargetCount\":" + value.maxTargetCount()
                + ",\"projectileId\":" + quote(value.projectileId())
                + ",\"animationKey\":" + quote(value.animationKey())
                + ",\"vfxKey\":" + quote(value.vfxKey())
                + ",\"sfxKey\":" + quote(value.sfxKey())
                + ",\"enabled\":" + value.enabled() + "}";
    }

    private static String alienSkillJson(AlienSkillLink value) {
        return "{\"alienId\":" + value.alienId()
                + ",\"skillId\":" + quote(value.skillId())
                + ",\"slotIndex\":" + value.slotIndex()
                + ",\"castPriority\":" + value.castPriority()
                + ",\"enabled\":" + value.enabled() + "}";
    }

    private static String projectileJson(ProjectileSpec value) {
        return "{\"projectileId\":" + quote(value.projectileId())
                + ",\"prefabKey\":" + quote(value.prefabKey())
                + ",\"moveType\":" + quote(value.moveType())
                + ",\"speed\":" + number(value.speed())
                + ",\"lifetimeSeconds\":" + number(value.lifetimeSeconds())
                + ",\"hitRadius\":" + number(value.hitRadius())
                + ",\"pierceCount\":" + value.pierceCount()
                + ",\"destroyOnHit\":" + value.destroyOnHit()
                + ",\"lostTargetPolicy\":" + quote(value.lostTargetPolicy())
                + ",\"enabled\":" + value.enabled() + "}";
    }

    private static String skillEffectJson(SkillEffectSpec value) {
        return "{\"skillId\":" + quote(value.skillId())
                + ",\"executionOrder\":" + value.executionOrder()
                + ",\"triggerPhase\":" + quote(value.triggerPhase())
                + ",\"effectType\":" + quote(value.effectType())
                + ",\"magnitudeSource\":" + quote(value.magnitudeSource())
                + ",\"baseMagnitude\":" + number(value.baseMagnitude())
                + ",\"coefficient\":" + number(value.coefficient())
                + ",\"chance\":" + number(value.chance())
                + ",\"durationSeconds\":" + number(value.durationSeconds())
                + ",\"tickIntervalSeconds\":" + number(value.tickIntervalSeconds())
                + ",\"radius\":" + number(value.radius())
                + ",\"maxStacks\":" + value.maxStacks()
                + ",\"stackPolicy\":" + quote(value.stackPolicy())
                + ",\"bossMultiplier\":" + number(value.bossMultiplier()) + "}";
    }

    private static String number(double value) {
        if (!Double.isFinite(value)) throw new BalanceConversionException("JSON에 NaN/Infinity를 기록할 수 없습니다.");
        return BigDecimal.valueOf(value).stripTrailingZeros().toPlainString();
    }

    private static String quote(String value) {
        StringBuilder escaped = new StringBuilder("\"");
        for (int index = 0; index < value.length(); index++) {
            char character = value.charAt(index);
            switch (character) {
                case '\\' -> escaped.append("\\\\");
                case '"' -> escaped.append("\\\"");
                case '\n' -> escaped.append("\\n");
                case '\r' -> escaped.append("\\r");
                case '\t' -> escaped.append("\\t");
                default -> {
                    if (character < 0x20) escaped.append(String.format("\\u%04x", (int) character));
                    else escaped.append(character);
                }
            }
        }
        return escaped.append('"').toString();
    }

    private static String sha256(String payload) {
        try {
            byte[] digest = MessageDigest.getInstance("SHA-256").digest(payload.getBytes(StandardCharsets.UTF_8));
            StringBuilder hex = new StringBuilder(64);
            for (byte value : digest) hex.append(String.format("%02x", value));
            return hex.toString();
        } catch (NoSuchAlgorithmException exception) {
            throw new IllegalStateException("SHA-256 is unavailable", exception);
        }
    }

    private static void writeUtf8Atomic(Path target, String contents) {
        Path temp = null;
        try {
            Files.createDirectories(target.getParent());
            temp = Files.createTempFile(target.getParent(), "battle-balance-", ".tmp");
            Files.writeString(temp, contents, StandardCharsets.UTF_8);
            try {
                Files.move(temp, target, StandardCopyOption.ATOMIC_MOVE, StandardCopyOption.REPLACE_EXISTING);
            } catch (AtomicMoveNotSupportedException ignored) {
                Files.move(temp, target, StandardCopyOption.REPLACE_EXISTING);
            }
        } catch (IOException exception) {
            if (temp != null) try { Files.deleteIfExists(temp); } catch (IOException ignored) {}
            throw new BalanceConversionException("Battle JSON을 기록할 수 없습니다: " + target, exception);
        }
    }

    public record FileDefinition<T>(
            String resourcePath,
            String fileName,
            Function<Data, List<T>> selector,
            Comparator<T> comparator,
            Function<T, String> serializer) {}

    public record DocumentOutput(
            String resourcePath,
            String fileName,
            String contentHash,
            String canonicalPayload,
            String json) {}

    public record WriteResult(Map<String, DocumentOutput> documents, String bundleHash, String manifestJson) {}
}
