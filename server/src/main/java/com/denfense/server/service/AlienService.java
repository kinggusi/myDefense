package com.denfense.server.service;

import com.denfense.server.domain.UserAlien;

import com.denfense.server.dto.response.AlienUpgradeResponseDto;

public interface AlienService {

    AlienUpgradeResponseDto upgradeAlien(String username, int alienId);
}
