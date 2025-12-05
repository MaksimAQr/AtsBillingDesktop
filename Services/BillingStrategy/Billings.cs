using System.Text;
using System;

namespace ATS_Desktop.Models.BillingStrategy;

class RegularBilling : IBillingStrategy, IBillable, IReportable, IAuditable 
{
    private double totalRevenue;
    private int totalMinutes;
    private DateTime lastActivity;
   
    public RegularBilling()
    {
        totalRevenue = 0;
        totalMinutes = 0;
        lastActivity = DateTime.Now;
    }

    public virtual double calculateCost(double baseCost, int minutes) => baseCost * minutes;

    public virtual string getStrategyName()
    {
        return "Regular";
    }

    public string getInvoice()
    {
        return "";
    }

    public string generateReport()
    {
        return "";
    }
    public DateTime getLastActivity()
    {
        return DateTime.Now;
    }

    public void audit()
    {
        return;
    }
    
}

class PreferentialBilling : RegularBilling, IDiscountable
{

    public PreferentialBilling(double discount) : base()
    {
        applyDiscount(discount);
    }

    private double discount {get; set;}
    public override double calculateCost(double baseCost, int minutes)  {
        double price = base.calculateCost(baseCost, minutes);
        return price * (1 - discount);
    }
    public void applyDiscount(double percentage)
    {
        this.discount = percentage / 100;
    }
    public double getCurrentDiscount() => this.discount;
    public override string getStrategyName()
    {
        return "Preferential";
    }
}