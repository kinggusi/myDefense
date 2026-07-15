package com.denfense.server.service;

import com.denfense.server.domain.UserAlien;

import com.denfense.server.dto.response.AlienUpgradeResponseDto;
import com.denfense.server.dto.response.AlienUpgradeStatusResponseDto;

public interface AlienService {

    AlienUpgradeResponseDto upgradeAlien(String username, int alienId);

    AlienUpgradeStatusResponseDto getUpgradeStatus(String username, int alienId);
}
