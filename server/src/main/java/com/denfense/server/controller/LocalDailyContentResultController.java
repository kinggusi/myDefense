package com.denfense.server.controller;

import com.denfense.server.dto.DailyContentDtos;
import com.denfense.server.service.DailyContentService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.context.annotation.Profile;
import org.springframework.web.bind.annotation.*;
import jakarta.servlet.http.HttpServletRequest;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;

import java.net.InetAddress;

@RestController
@RequiredArgsConstructor
@Profile({"local", "dev"})
@RequestMapping("/api/dev/daily-contents/results")
public class LocalDailyContentResultController {
    private final DailyContentService service;

    @PostMapping
    public DailyContentDtos.RunResponse submit(HttpServletRequest servletRequest,
                                               @Valid @RequestBody DailyContentDtos.ResultRequest request) {
        if (!isLoopback(servletRequest.getRemoteAddr())) {
            throw new BusinessException(ErrorCode.DAILY_CONTENT_RESULT_FORBIDDEN);
        }
        return service.submitResult(request);
    }

    private static boolean isLoopback(String address) {
        try {
            return address != null && InetAddress.getByName(address).isLoopbackAddress();
        } catch (Exception exception) {
            return false;
        }
    }
}
