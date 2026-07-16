package com.denfense.server.balance;

import java.util.List;

public record MonsterSpecBalanceDocument(List<MonsterSpecBalance> monsters) {
    public MonsterSpecBalanceDocument {
        monsters = monsters == null ? null : List.copyOf(monsters);
    }
}
