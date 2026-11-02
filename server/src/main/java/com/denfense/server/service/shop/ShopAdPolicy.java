package com.denfense.server.service.shop;
import com.denfense.server.auth.AuthEnvironment;
import org.springframework.beans.factory.ObjectProvider;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;
import com.denfense.server.domain.shop.ShopAdChallenge;

@Component
public class ShopAdPolicy {
    private final AuthEnvironment environment;
    private final ObjectProvider<RewardedAdVerifier> provider;
    @Value("${mydefense.shop.development-ads-enabled:false}") private boolean developmentEnabled;
    public ShopAdPolicy(AuthEnvironment environment,ObjectProvider<RewardedAdVerifier> provider) { this.environment=environment;this.provider=provider; }
    public boolean development() { return environment.isDevelopment() && developmentEnabled; }
    public boolean available() { var p=provider.getIfAvailable();return development() || (p!=null && p.available()); }
    public String verify(ShopAdChallenge challenge) {
        if(challenge.isDevelopment()) return development() && challenge.isVerified() ? challenge.getProviderTransactionId() : null;
        var p=provider.getIfAvailable();return p!=null && p.available() ? p.verifiedTransactionId(challenge) : null;
    }
}
