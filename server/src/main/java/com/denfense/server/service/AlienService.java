package com.denfense.server.service;

import com.denfense.server.domain.UserAlien;

public interface AlienService {

    UserAlien aleinUpgrade(String username, int alienId);
}
