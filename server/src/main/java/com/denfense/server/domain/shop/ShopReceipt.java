package com.denfense.server.domain.shop;

import jakarta.persistence.*;
import lombok.*;
import java.util.UUID;

@Entity @Getter @NoArgsConstructor
@Table(name="shop_receipt",uniqueConstraints=@UniqueConstraint(columnNames={"user_id","request_id"}))
public class ShopReceipt {
    @Id @GeneratedValue(strategy=GenerationType.IDENTITY) private Long id;
    @Column(name="user_id",nullable=false) private long userId;
    @Column(name="request_id",nullable=false) private UUID requestId;
    @Column(nullable=false) private String operation;
    @Lob @Column(nullable=false) private String fingerprint;
    @Lob @Column(nullable=false) private String responseJson;
    public ShopReceipt(long userId,UUID requestId,String operation,String fingerprint,String responseJson) {
        this.userId=userId;this.requestId=requestId;this.operation=operation;this.fingerprint=fingerprint;this.responseJson=responseJson;
    }
}
