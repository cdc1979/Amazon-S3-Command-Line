using PowerArgs;

public static class PowerArgsHelper
{
    public static s3CommandObject GetArgs(string[] args)
    {
        try
        {
            return Args.Parse<s3CommandObject>(args);
        }
        catch (ArgException ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ArgUsage.GenerateUsageFromTemplate<s3CommandObject>());
            return new s3CommandObject();
        }
    }

    public static ConsoleString WriteArgs()
    {
        return ArgUsage.GenerateUsageFromTemplate<s3CommandObject>();
    }
}
