using LessIsMoore.Web.Controllers;
using LessIsMoore.Web.Models;
using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Support.UI;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace LessIsMoore.Test
{
    public class UnitTest1
    {
        private string _strKey = "";
        private string _strAPI = "https://api.microsofttranslator.com/v2/Http.svc/Translate?text={0}&to={1}";

        private LIM.Exam.Models.Exam PopulateQuestionsFromXML()
        {
            string strXMLPath =
                "<questions><question><text>Favorite Letter</text><answers><answer>A</answer><answer>B</answer><answer>Y</answer><answer>Z</answer></answers></question></questions>";

            LIM.Exam.Models.Exam azureExam = new LIM.Exam.Models.Exam();

            System.Xml.Linq.XDocument xdocument = System.Xml.Linq.XDocument.Parse(strXMLPath);

            azureExam.ExamQuestions =
                new LIM.Exam.ExamChecker().PopulateExamQuestionsFromXML(xdocument.Root.Elements("question"));

            return azureExam;
        }

        [Fact]
        [Trait("Category", "UnitTest")]
        public void VerifyExam_1_PopulateQuestions()
        {
            LIM.Exam.Models.Exam azureExam = PopulateQuestionsFromXML();
            Assert.True(azureExam.TotalQuestions == 1);
        }

        [Theory]
        [InlineData("b", "b")]
        [Trait("Category", "UnitTest")]
        public void VerifyExam_2_GradeExam(string strAnswer, string strResponse)
        {
            LIM.Exam.Models.Exam azureExam = PopulateQuestionsFromXML();

            azureExam.ExamQuestions.First()
                .ExamChoices.Where(x => x.Text.ToLower() == strAnswer)
                .First().IsCorrect = true;

            IEnumerable<LIM.Exam.Models.ExamResponse> rsps =
                LIM.Exam.ExamChecker.CollectExamResponses(azureExam, x => (x.Text.ToLower() == strResponse));

            int intTotalCorrectQuestions = LIM.Exam.ExamChecker.GradeExam(azureExam, rsps);

            Assert.True(intTotalCorrectQuestions >= azureExam.TotalQuestions);
        }

        [Theory]
        [InlineData("home")]
        [Trait("Category", "UnitTest")]
        public async void VerifyHomeLoads(string strPageName)
        {
            HomeController homeController = new HomeController();
            ViewResult result = await homeController.Index() as ViewResult;

            Assert.Equal(strPageName, result.ViewData["title"].ToString().ToLower());
        }

        //[Fact]
        //[Trait("Category", "UnitTest")]
        //public async void VerifyVergeNewsFeed()
        //{
        //    NewsFeed[] arrNewsFeeds = await new LessIsMoore.Web.BLL.NewsFeed().FetchVergeNewsFeed();
        //    Assert.True(arrNewsFeeds.Length > 0);
        //}

        [Fact]
        [Trait("Category", "UnitTest")]
        public async void VerifyAzureNewsFeed()
        {
            NewsFeed[] arrNewsFeeds = await new LessIsMoore.Web.BLL.NewsFeed().FetchAzureNewsFeed();
            Xunit.Assert.True(arrNewsFeeds.Length > 0);
        }

        [Theory]
        [InlineData("Less", "Moins", "fr-FR")]
        [InlineData("Less", "Menos", "es-ES")]
        [Trait("Category", "UnitTest")]
        public async void VerifyTranslationAPILogic(string strText, string strExpectedText, string strLangangue)
        {
            LIM.TextTranslator.TextTranslator inst_TextTranslator = new LIM.TextTranslator.TextTranslator();

            string strAuthToken = await inst_TextTranslator.GetAccessToken(_strKey);
            string strTranslation = await inst_TextTranslator.CallTranslateAPI(_strAPI, strAuthToken, strText, strLangangue);

            Xunit.Assert.Equal(strExpectedText, strTranslation);
        }

        //[Theory]
        //[InlineData("Unit Testing", "UnitTest: VSTS_UpdateWorkItem", "user story")]
        //[Trait("Category", "UnitTest")]
        //public void VSTS_UpdateWorkItem(string strTitle, string strError, string strWorkItemType)
        //{
        //    string url = @"";
        //    string pat = "";

        //    IEnumerable<VSTSWorkItem> wi = 
        //        new Web.BLL.VSTS(pat, url).SaveWorkItems(
        //            new VSTSWorkItem[] {
        //                new Web.Models.VSTSWorkItem()
        //                {
        //                    Comments = strError+"...1",
        //                    Title = strTitle,
        //                    Steps = "",
        //                    Type = strWorkItemType,
        //                    id = -1,
        //                    ProjectName = "JackFrost",
        //                    Priority = 1
        //                },
        //                new Web.Models.VSTSWorkItem()
        //                {
        //                    Comments = strError+"...2",
        //                    Title = strTitle,
        //                    Steps = "",
        //                    Type = strWorkItemType,
        //                    id = -1,
        //                    ProjectName = "JackFrost",
        //                    Priority = 1
        //                }
        //            }
        //        );

        //    Xunit.Assert.True(wi.Count() > 0);
        //}


        //==========================================================
        //private string _strBaseURL = "";

        [Theory]
        [InlineData("Joe Dirt", "Dirt.Joe@Microsoft.com")]
        [Trait("Category", "Selenium")]
        public void VerifyExamUI(string strName, string strEmail)
        {
            //wd = new ChromeDriver(chromeOptions);
            //_wd = new InternetExplorerDriver();

            using (IWebDriver _wd = new PhantomJSDriver())
            {
                IJavaScriptExecutor _js = (IJavaScriptExecutor)_wd;

                _wd.Navigate().GoToUrl(@"http://www.lessismoore.net/exam?id=1&sf=8d679ae7-e939-474c-a3ff-8501ee636b12");
                _wd.Manage().Timeouts().ImplicitWait = new System.TimeSpan(0, 0, 10);

                IWait<IWebDriver> wait = new WebDriverWait(_wd, System.TimeSpan.FromSeconds(5));

                _wd.FindElement(By.Id("txtName")).SendKeys(strName);
                _wd.FindElement(By.Id("txtEmail")).SendKeys(strEmail);

                int[] ctrlIDs = { 101, 202, 303, 401, 504, 604, 703, 801, 902, 1003 };

                foreach (int intID in ctrlIDs)
                {
                    _wd.FindElement(By.Id("answer_" + intID.ToString())).Click();
                    _js.ExecuteScript("window.scrollBy(0,300)");
                }

                _js.ExecuteScript("window.confirm = function(msg){return true;};");
                _wd.FindElement(By.Id("txtSubmit")).Click();

                //wait.Until(ExpectedConditions.AlertIsPresent());

                //_wd.SwitchTo().Alert().Accept();

                //wait.Until(ExpectedConditions.AlertIsPresent());

                //_wd.SwitchTo().Alert().Accept();
                wait.Until(ExpectedConditions.ElementExists(By.Id("hdnScore")));

                int intScore;
                int.TryParse(_wd.FindElement(By.Id("hdnScore")).GetAttribute("value"), out intScore);

                Assert.True(intScore >= ctrlIDs.Length);
            }
        }

    }
}
