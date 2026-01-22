package com.denfense.server.service;

import com.denfense.server.domain.UserAlien;
import com.denfense.server.dto.response.UserAlienResponseDto;

import java.util.List;

public interface ShopService {

    List<UserAlienResponseDto> gachaAlien(String userName, int cnt);
}
