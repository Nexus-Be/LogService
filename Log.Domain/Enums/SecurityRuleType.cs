namespace Log.Domain.Enums;

public enum SecurityRuleType
{
    MassDelete = 0,
    BalanceTampering = 1,
    PrivilegeEscalation = 2,
    SuspiciousLogin = 3,
    RateLimitExceeded = 4
}
