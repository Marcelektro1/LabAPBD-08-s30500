namespace Tutorial8.Utils;

public class DatabaseUtil
{
    public static string GetConnectionString()
    {
        return System.Configuration.ConfigurationManager.ConnectionStrings["db-mssql"].ConnectionString;
    }
}