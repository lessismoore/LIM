﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LessIsMoore.Web.Models
{
    public class ExamQuestion
    {
        public int ID { get; set; }

        public string Text { get; set; }

        public IList<ExamChoice> ExamChoices { get; set; }
    }
}