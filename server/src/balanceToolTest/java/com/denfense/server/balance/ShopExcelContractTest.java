package com.denfense.server.balance;
import com.denfense.server.balance.tool.*;
import com.denfense.server.service.balance.ShopBalanceValidator;
import org.junit.jupiter.api.Test;
import static org.assertj.core.api.Assertions.*;
class ShopExcelContractTest {
    @Test void canonicalWorkbookContainsApprovedShopPolicy(){var path="../balance/source/balance-data.xlsx";var catalog=new ShopExcelReader().read(path);
        var all=new ExcelBalanceReader(path).read();ShopBalanceValidator.validate(catalog,all.shopProducts(),all.alienSpecs());
        assertThat(catalog.offers()).hasSize(11);assertThat(catalog.policy().dailyAdRerolls()).isEqualTo(3);
        assertThat(catalog.policy().discountPrice()).isEqualTo(2700);assertThat(catalog.policy().luckyWeight()).isEqualTo(2000);
        assertThat(catalog.offers().stream().filter(o->o.offerId().equals("GOLD_PACK")).findFirst().orElseThrow().price()).isEqualTo(500);}
    @Test void invalidPolicyIsRejected(){var path="../balance/source/balance-data.xlsx";var catalog=new ShopExcelReader().read(path);var all=new ExcelBalanceReader(path).read();
        var bad=new ShopPolicyBalance("DEFAULT","UTC",2000,10000,3,"GACHA_TEN",2700,3,600);
        assertThatThrownBy(()->ShopBalanceValidator.validate(new ShopCatalogBalance(catalog.offers(),bad),all.shopProducts(),all.alienSpecs())).isInstanceOf(IllegalStateException.class);}
}
