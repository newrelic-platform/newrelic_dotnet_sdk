using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace NewRelic.Platform.Sdk.FunctionalTests
{
    [TestClass]
    [DeploymentItem(@"config/newrelic.json", "config")]
    [DeploymentItem(@"config/plugin.json", "config")]
    public class RunnerTest
    {
        [TestMethod]
        public void TestRunnerSetupAndRunSucceeds()
        {
            Runner runner = new Runner();

            runner.Add(new TestAgentFactory());
            runner.Add(new TestAgent("FunctionalTest3", 4));
            runner.Add(new TestAgent("FunctionalTest4", 5));

            runner.SetupAndRunWithLimit(1);

            Assert.AreEqual(4, runner.Agents.Count);
        }

        [TestMethod]
        public void TestRunnerSetupFailsWithNullAgent()
        {
            try
            {
                Runner runner = new Runner();
                Agent agent = null;
                runner.Add(agent);
                Assert.Fail("Runner should raise exception when null agent is passed");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
        }

        [TestMethod]
        public void TestRunnerReportContinuesWithNullAgentName()
        {
            Runner runner = new Runner();
            runner.Add(new TestAgent("", 4));
            runner.SetupAndRunWithLimit(1); // Should not raise an exception
        }

        [TestMethod]
        public void TestRunnerReportContinuesWithNegativeValue()
        {
            Runner runner = new Runner();
            runner.Add(new TestAgent("FunctionalTest", -10));
            runner.SetupAndRunWithLimit(1); // Should not raise an exception
        }
        [TestMethod]
        public void TestPollCycleRunsEvery60Seconds()
        {
            Runner runner = new Runner();
            runner.Add(new TestAgent("", 4));
            runner.SetupAndRun();
            Thread.Sleep(10* 60 * 1000 + 50); //sleep for a little over 10 minutes
            Assert.AreEqual(runner.PollCycleCounter, 10); //check if 10 transmissions have occured
            

        }
    }
}
