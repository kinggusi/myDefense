package com.denfense.server.service.balance;

import org.springframework.stereotype.Component;

import java.util.List;

@Component
public class BalanceVersionRegistry {

    private volatile BalanceManifest manifest;

    public synchronized void init(BalanceManifest manifest) {
        if (this.manifest != null) {
            throw new IllegalStateException("BalanceVersionRegistry is already initialized.");
        }
        this.manifest = manifest;
    }

    public int getSchemaVersion() {
        return requireManifest().schemaVersion();
    }

    public String getBalanceVersion() {
        return requireManifest().balanceVersion();
    }

    public String getContentHash() {
        return requireManifest().contentHash();
    }

    public List<BalanceManifestFileEntry> getFiles() {
        return requireManifest().files();
    }

    private BalanceManifest requireManifest() {
        BalanceManifest current = manifest;
        if (current == null) {
            throw new IllegalStateException("Balance manifest is not initialized.");
        }
        return current;
    }
}
