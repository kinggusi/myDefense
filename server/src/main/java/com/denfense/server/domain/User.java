package com.denfense.server.domain;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.util.ArrayList;
import java.util.List;

@Entity
@Getter
@Setter
@NoArgsConstructor
@Table(name = "users") // DB 테이블 이름 지정
public class User {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    private String username;
    private String password;
    private int gold;
    private int diamond;

    @OneToMany(mappedBy = "user", cascade = CascadeType.ALL)
    private List<UserAlien> userAliens = new ArrayList<>();

    public User(String username, String password) {
        this.username = username;
        this.password = password;
        this.gold = 0; // 초기 골드는 0으로 시작 (나중에 setGold로 수정 가능)
    }


    public void decreaseDiamond(int amount){
        int diamond = this.gold - amount;
        if (diamond < 0) {
            throw new IllegalStateException("다이아가 부족합니다.");
        }
        this.gold = diamond;
    }

}