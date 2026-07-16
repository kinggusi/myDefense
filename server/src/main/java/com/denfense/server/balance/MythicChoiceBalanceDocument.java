package com.denfense.server.balance;

import java.util.List;

public record MythicChoiceBalanceDocument(List<MythicChoiceBalance> mythicChoices) {
    public MythicChoiceBalanceDocument {
        mythicChoices = mythicChoices == null ? null : List.copyOf(mythicChoices);
    }
}
