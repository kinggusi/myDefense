package com.denfense.server.domain.shop;

import jakarta.persistence.*;
import lombok.*;
import java.time.Instant;
import java.util.UUID;

@Entity @Getter @NoArgsConstructor
@Table(name="shop_ad_challenge",uniqueConstraints={@UniqueConstraint(columnNames={"user_id","request_id"}),
        @UniqueConstraint(columnNames={"provider_transaction_id"})})
public class ShopAdChallenge {
    @Id @GeneratedValue(strategy=GenerationType.IDENTITY) private Long id;
    @Column(name="challenge_id",unique=true,nullable=false) private UUID challengeId;
    @Column(name="user_id",nullable=false) private long userId;
    @Column(name="request_id",nullable=false) private UUID requestId;
    @Column(nullable=false) private String dayKey;
    @Column(nullable=false) private Instant expiresAt;
    private boolean development;
    private boolean verified;
    private boolean consumed;
    @Column(name="provider_transaction_id") private String providerTransactionId;
    public ShopAdChallenge(long userId,UUID requestId,String dayKey,Instant expiresAt,boolean development) {
        this.challengeId=UUID.randomUUID();this.userId=userId;this.requestId=requestId;this.dayKey=dayKey;
        this.expiresAt=expiresAt;this.development=development;
    }
    public void verify(String providerTransactionId) { this.verified=true;this.providerTransactionId=providerTransactionId; }
    public void consume() { this.consumed=true; }
}
