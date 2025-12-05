using System;
using ATS_Desktop.Models.BillingStrategy;

namespace ATS_Desktop.Models;

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