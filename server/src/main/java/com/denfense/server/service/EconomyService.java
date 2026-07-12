package com.denfense.server.service;

import com.denfense.server.dto.response.EconomyBalanceResponseDto;

public interface EconomyService {
    EconomyBalanceResponseDto getBalance(String username);
}
