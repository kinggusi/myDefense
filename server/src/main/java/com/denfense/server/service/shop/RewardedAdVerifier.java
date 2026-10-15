package com.denfense.server.service.shop;
import com.denfense.server.domain.shop.ShopAdChallenge;
/** Production implementation must verify server-to-server provider evidence bound to this challenge/user.
 * Client completion booleans and client-supplied transaction IDs are not evidence. */
public interface RewardedAdVerifier {
    boolean available();
    String verifiedTransactionId(ShopAdChallenge challenge);
}
