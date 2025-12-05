using ATS_Desktop.Models.BillingStrategy;
using System;

namespace ATS_Desktop.Models;
public partial class Tariff
{
    internal string name;
    internal double baseCostPerMinute;
    IBillingStrategy _strategy;
    internal string description { get; set; } = string.Empty;
    public bool HasDescription => !string.IsNullOrEmpty(description);

    protected IBillingStrategy Strategy 
    {
        get => _strategy;
        set =>_strategy = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Tariff(string tariffname, double cost, in IBillingStrategy strategy, string description = "")
    {
        name = tariffname;
        baseCostPerMinute = cost;
        Strategy = strategy;
        this.description = description;
    }

    public static bool operator >(Tariff t1, Tariff t2)
        => t1.baseCostPerMinute > t2.baseCostPerMinute;
        
    public static bool operator <(Tariff t1, Tariff t2)
        => t1.baseCostPerMinute < t2.baseCostPerMinute;
        
    public static bool operator >=(Tariff t1, Tariff t2)
        => t1.baseCostPerMinute >= t2.baseCostPerMinute;
        
    public static bool operator <=(Tariff t1, Tariff t2)
        => t1.baseCostPerMinute <= t2.baseCostPerMinute;

    public static bool operator ==(Tariff t1, Tariff t2)
    {
        if (ReferenceEquals(t1, t2)) return true;
        if (t1 is null || t2 is null) return false;
        
        return t1.name == t2.name && 
               t1.baseCostPerMinute == t2.baseCostPerMinute && 
               t1.description == t2.description;
    }
    
    public static bool operator !=(Tariff t1, Tariff t2)
    {
        return !(t1 == t2);
    }
        /*
    public static int operator +(Tariff t1, Tariff t2)
    {
        if(t1.Strategy.GetType() != t2.Strategy.GetType())
        {
            throw new InvalidOperationException("Cannot add tariffs of different types.");
        }
        double cost = t1.baseCostPerMinute + t2.baseCostPerMinute;
        if(t1.Strategy is PreferentialBilling && t2.Strategy is PreferentialBilling)
        {
            var discount1 = t1.Strategy.getCurrentDiscount();
            var discount2 = t2.Strategy.getCurrentDiscount();
            double averageDiscount = (discount1 + discount2) / 2;
            cost *= averageDiscount;
        }
        string name = t1.name + " & " + t2.name;
        return new Tariff(name, cost, t1.Strategy);
    }
    */
         
    public override bool Equals(object obj)
    {
        if (obj is Tariff other)
        {
            return this == other;
        }
        return false;
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(name, baseCostPerMinute, description);
    }    
    
    public double calculateCost(int minutes) => Strategy.calculateCost(baseCostPerMinute, minutes);
    public partial void setStrategy(IBillingStrategy newStrategy);
    public partial string getName();
    public partial double getBaseCost();
    public partial string getStrategyName();
    public partial void displayInfo();
}