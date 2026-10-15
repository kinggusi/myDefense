package com.denfense.server.controller;
import com.denfense.server.auth.*;
import com.denfense.server.service.shop.ShopCatalogService;
import org.springframework.web.bind.annotation.*;
import org.springframework.context.annotation.Profile;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import jakarta.servlet.http.HttpServletRequest;
import lombok.RequiredArgsConstructor;
import java.util.UUID;

@RestController @RequestMapping("/api/shop/development/ad-challenges") @RequiredArgsConstructor
@Profile("(local | dev) & !prod & !production")
@ConditionalOnProperty(name="mydefense.shop.development-ads-enabled",havingValue="true")
public class ShopDevelopmentAdController {
    private final AccountAccess accounts;
    private final AuthEnvironment environment;
    private final ShopCatalogService shop;
    @PostMapping("/{id}/complete") public void complete(@PathVariable UUID id,HttpServletRequest request){
        long uid=accounts.requirePrincipal().userId();
        if(!environment.allowsLegacy(request))throw AuthException.forbidden();
        shop.completeDevelopmentAd(uid,id);
    }
}
