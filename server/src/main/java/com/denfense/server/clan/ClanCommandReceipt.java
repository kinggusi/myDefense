package com.denfense.server.clan;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import java.time.Instant;
import java.util.UUID;

@Entity @Table(name="clan_command_receipts",uniqueConstraints=@UniqueConstraint(name="uk_clan_request",columnNames={"user_id","request_id"}))
@Getter @NoArgsConstructor
public class ClanCommandReceipt {
    @Id @GeneratedValue(strategy=GenerationType.IDENTITY) private Long id;
    @Column(name="user_id",nullable=false) private long userId;
    @Column(name="request_id",nullable=false) private UUID requestId;
    @Column(nullable=false,length=64) private String fingerprint;
    @Column(nullable=false,length=32) private String operation;
    @Column(nullable=false) private Instant processedAt;
    ClanCommandReceipt(long userId,UUID requestId,String fingerprint,String operation,Instant now) {
        this.userId=userId;this.requestId=requestId;this.fingerprint=fingerprint;this.operation=operation;processedAt=now;
    }
}
