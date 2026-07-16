package com.denfense.server.service.balance;

import com.fasterxml.jackson.annotation.JsonPropertyOrder;

import java.util.List;

@JsonPropertyOrder({"schemaVersion", "balanceVersion", "contentHash", "files"})
public record BalanceManifest(
        int schemaVersion,
        String balanceVersion,
        String contentHash,
        List<BalanceManifestFileEntry> files
) {
    public BalanceManifest {
        files = files == null ? List.of() : List.copyOf(files);
    }
}
