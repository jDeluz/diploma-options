﻿using DiplomaDataModel;
using DiplomaDataModel.Diploma;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using System.Web.Mvc;

namespace OptionsWebAPI.Controllers
{
    [EnableCors("*" , "*", "*")]
    public class ChoicesController : ApiController
    {
        DiplomasContext db = new DiplomasContext();
        
        public JObject GetGraphData()
        {
            JObject allChoices = new JObject();
            JObject choiceNum = new JObject();
            int[] arr_choice;
            var options = db.Options.Select(o => o.OptionID).ToArray();
            int[] arr_choices;

            var option_names = db.Options.Select(o => o.Title).ToArray();
            var enabled_option_names = db.Options.Where(o => o.isActive == true).Select(o => o.Title).ToArray();
            JArray ja_enabled_options = new JArray();
            ja_enabled_options.Add(enabled_option_names);
            JArray ja_options = new JArray();
            ja_options.Add(option_names);

            int?[] choice1;
            int?[] choice2;
            int?[] choice3;
            int?[] choice4;
            int?[][] choices;
            JArray c1;
            int[] allTermIDs = db.YearTerms.Select(t => t.YearTermID).ToArray();
            int[] years      = db.YearTerms.Select(t => t.Year).ToArray();
            int[] terms      = db.YearTerms.Select(t => t.Term).ToArray();
            int yearterm_num = 0;

            for (int b = 0; b < allTermIDs.Length; b++){
                yearterm_num = allTermIDs[b];
                choice1 = db.Choices.Where(c => c.YearTermID == yearterm_num).Select(c => c.FirstChoiceOptionId).ToArray();
                choice2 = db.Choices.Where(c => c.YearTermID == yearterm_num).Select(c => c.SecondChoiceOptionId).ToArray();
                choice3 = db.Choices.Where(c => c.YearTermID == yearterm_num).Select(c => c.ThirdChoiceOptionId).ToArray();
                choice4 = db.Choices.Where(c => c.YearTermID == yearterm_num).Select(c => c.FourthChoiceOptionId).ToArray();

                //An array that contains arrays of choices
                choices = new int?[][] { choice1, choice2, choice3, choice4 };
                choiceNum = new JObject();
                //Iterate through each choices: 1-4
                for (int k = 0; k < choices.Length; k++)
                {
                    c1 = new JArray();
                    arr_choices = new int[options.Length];
                    arr_choice = new int[options.Length + 1];   //Array holder for current iteration of all choices
                    for (int j = 0; j < choices[k].Length; j++) //Iterate through individual arrays of choices, 1st 2nd 3rd 4th choices
                        arr_choice[(int)choices[k][j]]++;  //Increment any repeating choices

                    Array.Copy(arr_choice, 1, arr_choices, 0, options.Length);
                    c1.Add(arr_choices);

                    choiceNum.Add("Choice" + (k + 1), c1);
                }
                allChoices.Add(yearterm_num.ToString(), choiceNum);
            }
            allChoices.Add("Options", ja_options);
            allChoices.Add("EnabledOptions", ja_enabled_options);

            return allChoices;
        }

        // POST api/choices
        [ResponseType(typeof(Choice))]
        public IHttpActionResult PostChoice(Choice choice)
        {
            JObject allMessages = new JObject();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            bool isValid = true;
            if (choicesHaveZero(choice))
            {
                JArray message = new JArray();
                message.Add("You must fill out all 4 option selections");
                allMessages.Add("Error", message);
                isValid = false;
            }
            if (!validChoices(choice))
            {
                JArray message = new JArray();
                message.Add("The options you chose must be all different");
                allMessages.Add("Error", message);
                isValid = false;
            }

            choice.SelectionDate = DateTime.Now;

            if (isValid)
            {
                db.Choices.Add(choice);
                db.SaveChanges();
                return CreatedAtRoute("DefaultApi", new { id = choice.ChoiceID }, choice);
            } else
            {
                JObject modelStateError = new JObject();
                modelStateError.Add("ModelState", allMessages);
                return Content(HttpStatusCode.BadRequest, modelStateError);
            }
        }

        // GET api/values/5
        public JObject Get(int id)
        {
            JObject students = new JObject();
            JArray ja_options = new JArray();
            string[] student_prop = new string[10];
            var allStudents = db.Choices.Where(o => o.YearTermID == id).ToArray();
            var option_names = db.Options.Select(o => o.Title).ToArray();
            int i = 0;
            

            foreach(var student in allStudents)
            {
                ja_options = new JArray();
                student_prop[0] = student.YearTermID.ToString();
                student_prop[1] = student.StudentID;
                student_prop[2] = student.StudentFirstName;
                student_prop[3] = student.StudentLastName;
                student_prop[4] = db.Options.Where(o => o.OptionID == student.FirstChoiceOptionId).Select(o => o.Title).First();
                student_prop[5] = db.Options.Where(o => o.OptionID == student.SecondChoiceOptionId).Select(o => o.Title).First();
                student_prop[6] = db.Options.Where(o => o.OptionID == student.ThirdChoiceOptionId).Select(o => o.Title).First();
                student_prop[7] = db.Options.Where(o => o.OptionID == student.FourthChoiceOptionId).Select(o => o.Title).First();
                student_prop[8] = student.SelectionDate.ToString();
                student_prop[9] = student.ChoiceID.ToString();

                ja_options.Add(student_prop);
                students.Add("student" + i++ , ja_options);
            }       
            return students;
        }

        private bool choicesHaveZero(Choice choice)
        {
            // Make sure the values aren't zero
            if (choice.FirstChoiceOptionId == 0
                || choice.SecondChoiceOptionId == 0
                || choice.ThirdChoiceOptionId == 0
                || choice.FourthChoiceOptionId == 0)
            {
                return true;
            } else
            {
                return false;
            }
        }

        // Check to make sure that the user has entered unique choices
        private bool validChoices(Choice choice)
        {
            // Make sure the values aren't null
            if (choice.FirstChoiceOptionId == null
                || choice.SecondChoiceOptionId == null
                || choice.ThirdChoiceOptionId == null
                || choice.FourthChoiceOptionId == null)
            {
                return false;
            }

            // Check for non-duplicate options
            var list = new List<int>();
            list.Add((int)choice.FirstChoiceOptionId);
            list.Add((int)choice.SecondChoiceOptionId);
            list.Add((int)choice.ThirdChoiceOptionId);
            list.Add((int)choice.FourthChoiceOptionId);

            if (list.Count != list.Distinct().Count())
            {
                return false;
            }
            else
            {
                return true;
            }
        }

    }
}
