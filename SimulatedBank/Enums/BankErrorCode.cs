namespace SimulatedBank.Enums
{
    public enum BankErrorCode
    {
        None,

        AccountNotFound,
        AccountInActive,

        InsufficientFunds,

        DuplicateRequest,

        ConcurrencyConflict,

        InvalidAmount,

        InternalError,
        InvalidRequest

    }
}
