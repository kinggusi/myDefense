package com.denfense.server.tutorial;
import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import java.util.UUID;

@Entity @Getter @NoArgsConstructor
@Table(name="tutorial_receipt",uniqueConstraints=@UniqueConstraint(columnNames={"user_id","request_id"}))
public class TutorialReceipt {
    @Id @GeneratedValue(strategy=GenerationType.IDENTITY) private Long id;
    @Column(name="user_id",nullable=false) private long userId;
    @Column(name="request_id",nullable=false) private UUID requestId;
    @Column(nullable=false,length=256) private String fingerprint;
    public TutorialReceipt(long userId,UUID requestId,String fingerprint) { this.userId=userId;this.requestId=requestId;this.fingerprint=fingerprint; }
}
