using System;
using ATS_Desktop.Services.BillingStrategy;

namespace ATS_Desktop.Services;

public partial class Tariff
{
    public partial void setStrategy(IBillingStrategy newStrategy)
    {
        Strategy = newStrategy; 
    }
    public partial string getName() => name;
    public partial double getBaseCost() => baseCostPerMinute;
    public partial string getStrategyName() => Strategy.getStrategyName();
    public partial void displayInfo()
    {
        Console.WriteLine($"Tariff: {name} | BaseCost: {baseCostPerMinute}/min | Strategy: {getStrategyName()}"); 
    }
}