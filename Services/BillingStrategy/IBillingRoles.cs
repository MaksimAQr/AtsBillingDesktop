using System;


namespace ATS_Desktop.Services.BillingStrategy;

public interface IBillable
{
    string getInvoice();
}

public interface IReportable
{
    string generateReport();
    DateTime getLastActivity();
}

public interface IAuditable
{
    void audit();
}

public interface IDiscountable
{
    void applyDiscount(double percentage);
    double getCurrentDiscount();
}