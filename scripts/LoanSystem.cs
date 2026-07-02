using System;
using System.Collections.Generic;

/// <summary>
/// 贷款系统 — 公司财务运营中的借贷机制
/// </summary>
public class LoanSystem
{
    /// <summary>未偿还本金</summary>
    public float Principal { get; set; }

    /// <summary>月利率 (0.005 ~ 0.02)</summary>
    public float InterestRate { get; set; }

    /// <summary>剩余还款月数</summary>
    public int RemainingMonths { get; set; }

    /// <summary>月供</summary>
    public float MonthlyPayment { get; set; }

    /// <summary>逾期月数（不还月供累计）</summary>
    public int OverdueMonths { get; set; }

    /// <summary>是否有活跃贷款</summary>
    public bool HasActiveLoan => Principal > 0 && RemainingMonths > 0;

    /// <summary>根据公司资产和声誉计算最大贷款额度</summary>
    public float GetMaxLoanAmount(float cash, float monthlyRevenue, int reputation, bool isListed)
    {
        float baseAmount = 200_000 + cash * 0.5f + monthlyRevenue * 12;
        float repMult = 1f + reputation / 100f;
        if (isListed) repMult += 0.5f;
        return baseAmount * repMult;
    }

    /// <summary>根据贷款额计算利率</summary>
    public float CalcInterestRate(float loanAmount, float maxLoan)
    {
        float ratio = loanAmount / maxLoan;
        if (ratio < 0.25f) return 0.005f;   // 小额 0.5%
        if (ratio < 0.50f) return 0.008f;   // 中额 0.8%
        if (ratio < 0.75f) return 0.012f;   // 大额 1.2%
        return 0.02f;                        // 顶额 2%
    }

    /// <summary>申请贷款</summary>
    public bool TakeLoan(float amount, float maxLoan)
    {
        if (HasActiveLoan) return false;
        ModAPI.FireHooks(ModAPI.GameHook.BeforeLoanTaken);
        if (ModAPI.IsCancelled(ModAPI.GameHook.BeforeLoanTaken)) return false;
        Principal = amount;
        InterestRate = CalcInterestRate(amount, maxLoan);
        RemainingMonths = 12;
        MonthlyPayment = amount * (1f + InterestRate * RemainingMonths) / RemainingMonths;
        OverdueMonths = 0;
        return true;
    }

    /// <summary>提前还款</summary>
    public float GetEarlyPayoff() => Principal;

    /// <summary>还清贷款</summary>
    public void PayOff() { ModAPI.FireHooks(ModAPI.GameHook.AfterLoanRepaid); Principal = 0; RemainingMonths = 0; OverdueMonths = 0; }

    /// <summary>每月处理 — 返回本月需支付的月供</summary>
    public float ProcessMonthly()
    {
        if (!HasActiveLoan) return 0;
        RemainingMonths--;
        if (RemainingMonths <= 0) { PayOff(); return 0; }
        return MonthlyPayment;
    }

    public Dictionary<string, object> Serialize()
    {
        return new()
        {
            ["principal"] = Principal,
            ["rate"] = InterestRate,
            ["months"] = RemainingMonths,
            ["payment"] = MonthlyPayment,
            ["overdue"] = OverdueMonths
        };
    }

    public static LoanSystem Deserialize(Dictionary<string, object> d)
    {
        return new()
        {
            Principal = Convert.ToSingle(d.GetValueOrDefault("principal", 0f)),
            InterestRate = Convert.ToSingle(d.GetValueOrDefault("rate", 0.01f)),
            RemainingMonths = Convert.ToInt32(d.GetValueOrDefault("months", 0)),
            MonthlyPayment = Convert.ToSingle(d.GetValueOrDefault("payment", 0f)),
            OverdueMonths = Convert.ToInt32(d.GetValueOrDefault("overdue", 0))
        };
    }
}
