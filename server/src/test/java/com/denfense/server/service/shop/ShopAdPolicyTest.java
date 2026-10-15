package com.denfense.server.service.shop;
import com.denfense.server.auth.AuthEnvironment;
import org.junit.jupiter.api.Test;
import org.springframework.mock.env.MockEnvironment;
import org.springframework.beans.factory.support.DefaultListableBeanFactory;
import org.springframework.test.util.ReflectionTestUtils;
import static org.assertj.core.api.Assertions.*;
class ShopAdPolicyTest {
    @Test void defaultIsFailClosedEvenInLocal(){assertThat(policy(false,"local").available()).isFalse();}
    @Test void productionAndMixedProfilesRejectDevelopmentMocks(){for(String[] profiles:new String[][]{{"prod"},{"production"},{"local","prod"},{"dev","production"},{}})
        assertThat(policy(true,profiles).available()).isFalse();}
    @Test void explicitLocalAndDevEnableMock(){assertThat(policy(true,"local").development()).isTrue();assertThat(policy(true,"dev").development()).isTrue();}
    private ShopAdPolicy policy(boolean enabled,String... profiles){var env=new MockEnvironment();env.setActiveProfiles(profiles);
        var result=new ShopAdPolicy(new AuthEnvironment(env),new DefaultListableBeanFactory().getBeanProvider(RewardedAdVerifier.class));
        ReflectionTestUtils.setField(result,"developmentEnabled",enabled);return result;}
}
