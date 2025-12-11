namespace ATS_Desktop.Services.BillingStrategy;

public interface IBillingStrategy
{
   double calculateCost(double baseCost, int minutes);
   string getStrategyName();
}