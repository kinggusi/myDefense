package com.denfense.server.service.balance;

import lombok.Getter;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.stream.Collectors;

@Getter
public class AlienSpecConsistencyResult {
    private final List<Long> missingIds = new ArrayList<>();
    private final List<Long> unknownIds = new ArrayList<>();
    private final List<FieldMismatch> fieldMismatches = new ArrayList<>();

    public void addMissingId(long alienId) {
        missingIds.add(alienId);
        Collections.sort(missingIds);
    }

    public void addUnknownId(long alienId) {
        unknownIds.add(alienId);
        Collections.sort(unknownIds);
    }

    public void addFieldMismatch(long alienId, String field, String jsonValue, String dbValue) {
        fieldMismatches.add(new FieldMismatch(alienId, field, jsonValue, dbValue));
        fieldMismatches.sort((m1, m2) -> {
            int idCompare = Long.compare(m1.alienId(), m2.alienId());
            if (idCompare != 0) return idCompare;
            return m1.field().compareTo(m2.field());
        });
    }

    public boolean isConsistent() {
        return missingIds.isEmpty() && unknownIds.isEmpty() && fieldMismatches.isEmpty();
    }

    public String buildSummaryMessage() {
        if (isConsistent()) {
            return "AlienSpec DB matches balance registry.";
        }
        StringBuilder sb = new StringBuilder("AlienSpec consistency check failed.\n");
        if (!missingIds.isEmpty()) {
            sb.append("Missing IDs (in JSON but not in DB): ").append(missingIds).append("\n");
        }
        if (!unknownIds.isEmpty()) {
            sb.append("Unknown IDs (in DB but not in JSON): ").append(unknownIds).append("\n");
        }
        if (!fieldMismatches.isEmpty()) {
            sb.append("Field Mismatches:\n");
            for (FieldMismatch m : fieldMismatches) {
                sb.append(String.format("- alienId=%d, field=%s, json=%s, db=%s\n",
                        m.alienId(), m.field(), m.jsonValue(), m.dbValue()));
            }
        }
        return sb.toString().trim();
    }

    public record FieldMismatch(long alienId, String field, String jsonValue, String dbValue) {}
}
