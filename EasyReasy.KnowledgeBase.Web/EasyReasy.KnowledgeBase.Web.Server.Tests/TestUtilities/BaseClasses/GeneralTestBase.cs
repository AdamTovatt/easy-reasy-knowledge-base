using EasyReasy.EnvironmentVariables;

namespace EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.BaseClasses
{
    public abstract class GeneralTestBase
    {
        protected static void LoadTestEnvironmentVariables()
        {
            EnvironmentVariableHelper.LoadVariablesFromFile("..\\..\\TestEnvironmentVariables.txt");
        }
    }
}
