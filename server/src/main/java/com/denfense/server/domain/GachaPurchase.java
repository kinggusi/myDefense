package com.denfense.server.domain;

import jakarta.persistence.*;
import lombok.AccessLevel;
import lombok.Getter;
import lombok.NoArgsConstructor;
import org.springframework.data.annotation.CreatedDate;
import org.springframework.data.jpa.domain.support.AuditingEntityListener;

import java.time.LocalDateTime;
import java.util.UUID;

@Entity
@Table(name = "gacha_purchase", uniqueConstraints = {
        @UniqueConstraint(name = "uk_gacha_purchase_user_request", columnNames = {"user_id", "purchase_request_id"})
})
@Getter
@NoArgsConstructor(access = AccessLevel.PROTECTED)
@EntityListeners(AuditingEntityListener.class)
public class GachaPurchase {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "user_id", nullable = false)
    private User user;

    @Column(name = "purchase_request_id", nullable = false)
    private UUID purchaseRequestId;

    @Column(name = "product_id", nullable = false)
    private String productId;

    @Enumerated(EnumType.STRING)
    @Column(name = "status", nullable = false)
    private GachaPurchaseStatus status;

    @Lob
    @Column(name = "response_json")
    private String responseJson;

    @CreatedDate
    @Column(name = "created_at", nullable = false, updatable = false)
    private LocalDateTime createdAt;

    public GachaPurchase(User user, UUID purchaseRequestId, String productId, GachaPurchaseStatus status) {
        this.user = user;
        this.purchaseRequestId = purchaseRequestId;
        this.productId = productId;
        this.status = status;
        this.createdAt = LocalDateTime.now();
    }

    public void complete(String responseJson) {
        this.status = GachaPurchaseStatus.COMPLETED;
        this.responseJson = responseJson;
    }
}
