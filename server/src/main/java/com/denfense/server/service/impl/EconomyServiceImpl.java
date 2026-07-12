package com.denfense.server.service.impl;

import com.denfense.server.domain.User;
import com.denfense.server.dto.response.EconomyBalanceResponseDto;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.EconomyService;
import com.denfense.server.service.HeartPolicy;
import com.denfense.server.service.HeartSnapshot;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.util.StringUtils;

@Service
@RequiredArgsConstructor
public class EconomyServiceImpl implements EconomyService {

    private final UserRepository userRepository;
    private final HeartPolicy heartPolicy;

    @Override
    @Transactional(readOnly = true)
    public EconomyBalanceResponseDto getBalance(String username) {
        if (!StringUtils.hasText(username)) {
            throw new BusinessException(ErrorCode.INVALID_REQUEST, "username이 비어있습니다.");
        }

        User user = userRepository.findByUsername(username)
                .orElseThrow(() -> new BusinessException(ErrorCode.USER_NOT_FOUND, "유저 없음"));

        HeartSnapshot snapshot = heartPolicy.calculate(user.getHeart(), user.getLastHeartUpdateTime());

        return EconomyBalanceResponseDto.builder()
                .username(user.getUsername())
                .accountGold(user.getGold())
                .gem(user.getDiamond())
                .heart(snapshot.calculatedHeart())
                .universalPiece(user.getUniversalPiece())
                .growthCell(user.getGrowthCell())
                .heartMax(HeartPolicy.MAX_HEART)
                .nextHeartRecoveryAt(snapshot.nextHeartRecoveryAt())
                .serverTime(snapshot.serverTime())
                .build();
    }
}
