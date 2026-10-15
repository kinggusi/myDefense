package com.denfense.server.domain.shop;

import jakarta.persistence.*;
import lombok.*;

/** Mutated only while holding the owning User row lock. No client JSON is stored here. */
@Entity @Getter @NoArgsConstructor
@Table(name="shop_state", uniqueConstraints=@UniqueConstraint(columnNames="user_id"))
public class ShopState {
    @Id @GeneratedValue(strategy=GenerationType.IDENTITY) private Long id;
    @Column(name="user_id",nullable=false) private long userId;
    @Lob @Column(nullable=false) private String stateJson;
    public ShopState(long userId, String stateJson) { this.userId=userId;this.stateJson=stateJson; }
    public void update(String json) { this.stateJson=json; }
}
