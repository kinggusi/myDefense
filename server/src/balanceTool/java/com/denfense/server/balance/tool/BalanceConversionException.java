package com.denfense.server.balance.tool;

public class BalanceConversionException extends RuntimeException {
    public BalanceConversionException(String message) {
        super(message);
    }
    
    public BalanceConversionException(String message, Throwable cause) {
        super(message, cause);
    }
}
