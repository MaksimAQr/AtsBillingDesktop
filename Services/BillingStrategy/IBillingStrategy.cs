namespace ATS_Desktop.Models.BillingStrategy;

public interface IBillingStrategy
{
   double calculateCost(double baseCost, int minutes);
   string getStrategyName();
}