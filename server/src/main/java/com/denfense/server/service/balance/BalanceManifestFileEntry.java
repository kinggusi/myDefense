package com.denfense.server.service.balance;

import com.fasterxml.jackson.annotation.JsonPropertyOrder;

@JsonPropertyOrder({"name", "sha256", "size"})
public record BalanceManifestFileEntry(String name, String sha256, long size) {
}
