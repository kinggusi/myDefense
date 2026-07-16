package com.denfense.server.balance;

import java.util.List;

public record FieldLimitBalanceDocument(List<FieldLimitBalance> fieldLimits) {
    public FieldLimitBalanceDocument {
        fieldLimits = fieldLimits == null ? null : List.copyOf(fieldLimits);
    }
}
