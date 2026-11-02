package com.denfense.server.service.shop;
import com.denfense.server.auth.*;
import com.denfense.server.controller.ShopDevelopmentAdController;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;
import org.springframework.boot.test.context.runner.ApplicationContextRunner;
import static org.assertj.core.api.Assertions.*;
import static org.mockito.Mockito.*;
class ShopDevelopmentControllerGuardTest {
    private final ApplicationContextRunner runner=new ApplicationContextRunner().withUserConfiguration(ShopDevelopmentAdController.class)
            .withBean(AccountAccess.class,()->mock(AccountAccess.class)).withBean(AuthEnvironment.class,()->mock(AuthEnvironment.class))
            .withBean(ShopCatalogService.class,()->mock(ShopCatalogService.class));
    @Test void defaultOff(){runner.withPropertyValues("spring.profiles.active=local").run(c->assertThat(c).doesNotHaveBean(ShopDevelopmentAdController.class));}
    @ParameterizedTest @ValueSource(strings={"prod","production","local,prod","local,production","dev,production",""})
    void deniedProfilesNeverRegisterController(String profile){runner.withPropertyValues("spring.profiles.active="+profile,"mydefense.shop.development-ads-enabled=true")
            .run(c->{assertThat(c).hasNotFailed();assertThat(c).doesNotHaveBean(ShopDevelopmentAdController.class);});}
    @ParameterizedTest @ValueSource(strings={"local","dev"})
    void developmentOptInRegistersActualController(String profile){runner.withPropertyValues("spring.profiles.active="+profile,"mydefense.shop.development-ads-enabled=true")
            .run(c->{assertThat(c).hasNotFailed();assertThat(c).hasSingleBean(ShopDevelopmentAdController.class);});}
}
