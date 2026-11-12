package com.denfense.server.auth;

import com.denfense.server.DefenseServerApplication;
import com.denfense.server.repository.UserRepository;
import org.springframework.boot.builder.SpringApplicationBuilder;
import org.springframework.transaction.PlatformTransactionManager;
import org.springframework.transaction.support.TransactionTemplate;
import java.util.UUID;

/** Test classpath only. Never connects to the local persistent or production databases. */
public final class ShopUiSmokeServer {
    public static void main(String[] args) {
        if (args.length != 1 || !"--shop-ui-smoke".equals(args[0]))
            throw new IllegalArgumentException("Explicit --shop-ui-smoke required");
        var context = new SpringApplicationBuilder(DefenseServerApplication.class).profiles("local")
                .run("--server.address=127.0.0.1", "--server.port=18084",
                        "--spring.datasource.url=jdbc:h2:mem:shop-ui-" + UUID.randomUUID() + ";MODE=MySQL;DB_CLOSE_DELAY=-1",
                        "--spring.jpa.hibernate.ddl-auto=create-drop", "--spring.jpa.show-sql=false", "--spring.sql.init.mode=never",
                        "--mydefense.shop.development-ads-enabled=true",
                        "--mydefense.auth.local-accounts.bootstrap-enabled=false", "--mydefense.collection.tutorial-mythic-preview-enabled=false");
        var writer = context.getBean(GuestAccountWriter.class);
        var users = context.getBean(UserRepository.class);
        var transaction = new TransactionTemplate(context.getBean(PlatformTransactionManager.class));
        String secret = "D".repeat(42) + "A";
        AuthSecrets.requireSecret(secret);
        long id = writer.findOrCreate(AuthSecrets.hash(secret));
        transaction.executeWithoutResult(tx -> {
            var user = users.findByIdForUpdate(id).orElseThrow();
            user.setAccountLevel(10); user.setGold(5000000); user.setDiamond(100000);
        });
        System.out.println("SHOP_UI_SMOKE_READY loopback=18084 accounts=1 temporary-memory-db=true");
    }
}
