package com.denfense.server.service.shop;
import org.springframework.stereotype.Component;
import java.security.SecureRandom;
@Component public class ShopRandomSource { private final SecureRandom random=new SecureRandom(); public int nextInt(int bound) { return random.nextInt(bound); } }
