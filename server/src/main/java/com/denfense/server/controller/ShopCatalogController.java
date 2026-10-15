package com.denfense.server.controller;
import com.denfense.server.auth.AccountAccess;
import com.denfense.server.dto.shop.ShopDtos.*;
import com.denfense.server.service.shop.ShopCatalogService;
import org.springframework.web.bind.annotation.*;
import lombok.RequiredArgsConstructor;

@RestController @RequestMapping("/api/shop") @RequiredArgsConstructor
public class ShopCatalogController {
    private final AccountAccess accounts;
    private final ShopCatalogService shop;
    @GetMapping("/catalog") public Catalog catalog(){return shop.catalog(accounts.requirePrincipal().userId());}
    @PostMapping("/purchases") public PurchaseResponse purchase(@RequestBody PurchaseRequest request){return shop.purchase(accounts.requirePrincipal().userId(),request);}
    @PostMapping("/reroll-challenges") public Challenge challenge(@RequestBody ChallengeRequest request){return shop.challenge(accounts.requirePrincipal().userId(),request==null?null:request.requestId());}
    @PostMapping("/rerolls") public RerollResponse reroll(@RequestBody RerollRequest request){return shop.reroll(accounts.requirePrincipal().userId(),request);}
}
