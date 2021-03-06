﻿using System;
using System.Linq;
using LIM.Exam.Web.Models;
using System.Xml.Linq;
//using StackExchange.Profiling;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;

namespace LIM.Exam.Web.Controllers
{
    public class ExamController : BaseController
    {
        private IHostingEnvironment _env;
        private AppSettings _AppSettings;
        private IHttpContextAccessor _context;

        public ExamController(IHostingEnvironment env, IOptions<AppSettings> settings, IHttpContextAccessor context = null)
        {
            this._env = env;
            _AppSettings = settings != null ? settings.Value : null;
            _context = context;
        }

        private LIM.Exam.Models.Exam PopulateExamQuestions(int intExamID, Action<LIM.Exam.Models.Exam> cb)
        {

            LIM.Exam.Models.Exam azureExam = new LIM.Exam.Models.Exam();

            cb(azureExam);

            string strXMLPath = null;

            azureExam.ID = intExamID;

            if (azureExam.ID == 1)
            {
                strXMLPath = "\\app_data\\AzureQuiz.xml";
                azureExam.Name = "Exam: Fast Start – Azure for Modern Web and Mobile Application Development";
            }
            else if (azureExam.ID == 2)
            {
                //..\\..\\..
                strXMLPath = "\\app_data\\DevopsQuiz.xml";
                azureExam.Name = "Exam: Fast Start – Azure for Dev Ops";
            }
            else if (azureExam.ID == 3)
            {
                //..\\..\\..
                strXMLPath = "\\app_data\\SDLQuiz.xml";
                azureExam.Name = "Exam: Security Development Lifecycle";
            }
            else
            {
                return azureExam;
            }

            XDocument xdocument = XDocument.Load(_env.ContentRootPath +strXMLPath);

            azureExam.ExamQuestions = 
                new LIM.Exam.ExamChecker().PopulateExamQuestionsFromXML(xdocument.Root.Elements("question"), azureExam.ShuffleQuestions, azureExam.ShuffleQuestionChoices);

            return azureExam;
        }

        [HttpGet]
        public IActionResult Index(int id)
        {
            _context.HttpContext.Session.Remove("AzureExam");

            bool NoShuffleQuestions = !(_context.HttpContext.Request.Query["sf"] == "8d679ae7-e939-474c-a3ff-8501ee636b12");

            LIM.Exam.Models.Exam azureExam = PopulateExamQuestions(id, (x=> {
                x.ShuffleQuestions = NoShuffleQuestions;
                x.ShuffleQuestionChoices = NoShuffleQuestions;
            }));

            _context.HttpContext.Session.SetString("AzureExam", JsonConvert.SerializeObject(azureExam));
            _context.HttpContext.Session.SetString("AzureExamStartTime", DateTime.Now.ToShortTimeString() );

            return View(azureExam);
        }

        [HttpPost]
        public async Task<IActionResult> Submit(string txtEmail, string txtName)
        {
            //var remoteIpAddress = HttpContext.Connection.RemoteIpAddress;

            txtName = txtName.Trim();
            txtEmail = txtEmail.Trim();

            if ((_context.HttpContext.Session.GetString("AzureExam") == null) || string.IsNullOrEmpty(txtName) || string.IsNullOrEmpty(txtEmail)) {
                return View("Index");
            }

            LIM.Exam.Models.Exam azureExam = JsonConvert.DeserializeObject<LIM.Exam.Models.Exam>(_context.HttpContext.Session.GetString("AzureExam"));

            azureExam.HasStarted = true;
            azureExam.TakerEmail = System.Net.WebUtility.HtmlEncode(txtEmail);
            azureExam.TakerName = System.Net.WebUtility.HtmlEncode(txtName);

            IEnumerable<LIM.Exam.Models.ExamResponse> rsps =
                LIM.Exam.ExamChecker.CollectExamResponses(azureExam, x => (Request.Form["answer_" + x.ID.ToString()] == "on"));

            int intTotalCorrectQuestions = LIM.Exam.ExamChecker.GradeExam(azureExam, rsps);
            TempData["TotalCorrectQuestions"] = intTotalCorrectQuestions;

            //========================================================
            DateTime dteAzureExamStartTime;
            DateTime.TryParse(_context.HttpContext.Session.GetString("AzureExamStartTime"), out dteAzureExamStartTime);

            TelemetryClient appInsights = new TelemetryClient();
            Dictionary<string, string> properties = new Dictionary<string, string>();
            var metrics = new Dictionary<string, double>();

            properties["Name: LessIsMoore Exam"] = azureExam.Name;
            metrics["Score: LessIsMoore Exam"] = intTotalCorrectQuestions;
            metrics["Duration(min): LessIsMoore Exam"] = (DateTime.Now - dteAzureExamStartTime).TotalMinutes;

            appInsights.TrackEvent("LessIsMoore Exam", properties, metrics);
            //========================================================

            if ((azureExam.TotalQuestions > 0) && (intTotalCorrectQuestions < azureExam.TotalQuestions))
                return View("Index", azureExam);

            //report/exam
            _context.HttpContext.Session.SetString("ExamReport", JsonConvert.SerializeObject(azureExam));
            await SendCertificationEmail(azureExam);

            this.TempData["HasPassed"] = "YES";
            this.TempData["Name"] = azureExam.TakerName;
            this.TempData["Email"] = azureExam.TakerEmail;

            return RedirectToAction("Index", new { ID = azureExam.ID });
        }

        private async Task SendCertificationEmail(LIM.Exam.Models.Exam azureExam)
        {
            await new LIM.SendGrid.SendGrid(_AppSettings.SendGridSettings).SendEmailAsync(
                            "",
                            "Certification Request",
                            azureExam.Name,
                            string.Format("New Request from {0} at Email: {1}", azureExam.TakerName, azureExam.TakerEmail)
                        );
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
