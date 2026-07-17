package com.denfense.server.balance;

import java.util.List;

public record MythicChoiceBalanceDocument(List<MythicChoiceBalance> mythicChoices, List<Long> excludedAlienIds) {
    public MythicChoiceBalanceDocument(List<MythicChoiceBalance> mythicChoices) { this(mythicChoices, List.of()); }
    public MythicChoiceBalanceDocument {
        mythicChoices = mythicChoices == null ? null : List.copyOf(mythicChoices);
        excludedAlienIds = excludedAlienIds == null ? List.of() : List.copyOf(excludedAlienIds);
    }
}
