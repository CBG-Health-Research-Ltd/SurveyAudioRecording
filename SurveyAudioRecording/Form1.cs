using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Net.Sockets;
using NAudio.Wave;
using System.Media;
using NAudio;
using System.Net;
using System.Windows.Forms;

namespace SurveyAudioRecording
{
    public partial class Form1 : Form
    {
        List<string[]> childY9ShowcardList;
        List<string[]> adultY9ShowcardList;
        List<string[]> childY10ShowcardList;
        List<string[]> adultY10ShowcardList;



        string[] subStrings;
        bool   recording;
        string latestFile;
        string record;//Used as a global to determine if the voice recording is still hapening in secret.
        string houseHoldID;

        public Form1()
        {
            
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false; // This is optional
            waveSource = new WaveInEvent();//Needs to be initialised for event firing inr ecording function.
            recording = false; //needs to be initialised orignially to false for correct functionality.
            InitialiseShowcards();
            InitializeComponent();
            closeFirstInstance();

        }

        private void InitialiseShowcards()
        {
            childY9ShowcardList = GetShowcardPageList("CHILDY9");
            adultY9ShowcardList = GetShowcardPageList("ADULTY9");
            childY10ShowcardList = GetShowcardPageList("CHILDY10");
            adultY10ShowcardList = GetShowcardPageList("ADULTY10");
        }

        private void closeFirstInstance()
        {
            Process[] pname = Process.GetProcessesByName(AppDomain.CurrentDomain.FriendlyName.Remove(AppDomain.CurrentDomain.FriendlyName.Length - 4));
            if (pname.Length > 1)
            {
                pname[1].Kill();
            }
        }

        private List<string[]> GetShowcardPageList(string survey)//takes the type of survey CHILD or ADULT to dtetermine list to load.
        {
            string User = Environment.UserName;
            string[] ShowcardPageArray = new string[0];

            try
            {
                switch (survey)
                {
                    case ("CHILDY9"):
                        ShowcardPageArray = File.ReadAllLines(@"C:\CBGShared\surveyinstructions\NZHSY9ChildInstructions.txt");
                        break;
                    case ("ADULTY9"):
                        ShowcardPageArray = File.ReadAllLines(@"C:\CBGShared\surveyinstructions\NZHSY9AdultInstructions.txt");
                        break;
                    case ("CHILDY10"):
                        ShowcardPageArray = File.ReadAllLines(@"C:\CBGShared\surveyinstructions\NZHSY10ChildInstructions.txt");
                        break;
                    case ("ADULTY10"):
                        ShowcardPageArray = File.ReadAllLines(@"C:\CBGShared\surveyinstructions\NZHSY10AdultInstructions.txt");
                        break;
                }
            }
            catch (Exception e)
            {
                //Missing some survey insructions files
                //Do nothing
            }
            finally
            {
                //Continue with app as expected. This allows code that was successfully executed to be stored
                //so that relevant show-cards will still be displayed.
            }



            List<string[]> shoPageList = new List<string[]>();
            char splitter = ' ';

            for (int i = 0; i < ShowcardPageArray.Length; i++)
            {
                //NOTE: Uses TSSQuestionNum -> RequiredShowcard rleationship. "&QN&\t&SN&"
                ShowcardPageArray[i] = ShowcardPageArray[i].Replace("\t", " ");//Replacing tab with space for ease of processing.
                //Could get rid of the above line if we have space delimited .txt file.
                subStrings = ShowcardPageArray[i].Split(splitter);//Forming array into sub strings so it may be added to list
                shoPageList.Add(subStrings);//Generating the pageNum -> ShoCard list.
            }
            return shoPageList;
        }

        private List<string[]> getShowcardList(string survey)
        {
            survey = survey.ToLower();
            List<string[]> showcardList = new List<string[]>();
            switch (survey)
            {
                case ("nzcy9"):
                    showcardList = childY9ShowcardList;
                    break;
                case ("nzay9"):
                    showcardList = adultY9ShowcardList;
                    break;
                case ("nha10"):
                    showcardList = childY9ShowcardList;
                    break;
                case ("nhc10"):
                    showcardList = adultY9ShowcardList;
                    break;

            }
            return showcardList;
        }

        static WaveFileWriter waveFile;
        static WaveInEvent waveSource;
        private void RecordInSecret()
        {

            //If the previous question was recording, then this will stop it from recording and dispose the 
            //waveSource. waveSource is re initialised when told to being recording again.
            if (recording == true)
            {
                waveSource.StopRecording();
                recording = false;
                waveFile.Dispose();
            }

            string userName = System.Environment.UserName;

            if (record == "record")
            {
                string info  = File.ReadLines(@"C:\CBGShared\AudioRecording\AudioFilename.txt").First();
                waveSource = new WaveInEvent();
                waveSource.WaveFormat = new WaveFormat(6500, 1);
                waveSource.DataAvailable += new EventHandler<WaveInEventArgs>(waveSource_DataAvailable);
                string fileName = latestFile.Replace(' ', '_') + "_" + DateTime.Now.ToString("h_mm_ss tt").Replace(' ', '_') + info;
                fileName = ProcessRecordedFileName(fileName);
                string tempFile = (@"C:\CBGShared\recordedquestions\" + fileName + ".wav");
                waveFile = new WaveFileWriter(tempFile, waveSource.WaveFormat);
                waveSource.StartRecording();
                recording = true;
            }

        }

        //Important for not wanting to name surveys per pageturner.txt convention. Allows you to customise
        //by modifying.
        private string ProcessRecordedFileName(string info)
        {
            //Just a big if statement for post-processing any filename text. Swap strings for other strings.
            
            if (info.Contains("NZCY9"))
            {
                return info.Replace("NZCY9", "NZHSCY9");
            }
            else if (info.Contains("NZAY9"))
            {
                return info.Replace("NZAY9", "NZHSAY9");
            }
            if (info.Contains("NHC10"))
            {
                return info.Replace("NHC10", "NZHSCY10");
            }
            else if (info.Contains("NHA10"))
            {
                return info.Replace("NHA10", "NZHSAY10");
            }
            else
            {
                return info;
            }
        }

        static void waveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            waveFile.WriteData(e.Buffer, 0, e.BytesRecorded);

        }

        private string ShouldYouRecord(string inputTxt) //This could be tidied up in the future.
        {
            //Obtain the question number and then find the corresponding showcard from look up.
            if (string.Equals(inputTxt.Substring(0, 8), "question", StringComparison.CurrentCultureIgnoreCase)) ; //Makesure question&QN& CHILD/ADULT
            {
                char Qsplitter = ' ';//Splitting at the space between e.g. "question14 CHILD/ADULT" (the raw survey args).
                string[] subStrings = inputTxt.Split(Qsplitter);
                string questionNum = subStrings[0].Substring(8); //Page number hardcoded to correspond to question number.
                string surveyInfo = subStrings[1];//Either CHILD or ADULT (determines which survey look up and PDF to use).
                //houseHoldID = subStrings[2];
                return SelectRecord(surveyInfo, questionNum);
            }
            return ("Error in instruction file");
        }


        private string SelectRecord(string surveyInfo, string questionNum)
        {
            string permission = ReadFirstLine(@"C:\CBGShared\RecordPermission\recordpermission.txt");
            string record = null; //Default record setting. Rif record keyword does not exist then this wil be returned.

            //Ensures that file creation is within two hour limit and respondne has consented to recording.
            if (permission.Contains("true") && CheckTimeStamp(@"C:\CBGShared\RecordPermission\recordpermission.txt", 2))
            {
                List<string[]> showcardList = getShowcardList(surveyInfo);
                int i = 0;
                while (i < showcardList.Count)
                {
                    if ((showcardList[i])[0] == questionNum)//Page num is first element i.e. 0 index of showcard list entries.
                    {
                        //Must ensure that length is greater than 2 in order to avoid index out of range exception.
                        //Any element in list with more than just QN->SPN will have greater than 2 length.
                        if ((showcardList[i].Length > 2) && showcardList[i][2] == "record")//Checks to see that record exists.

                        { record = (showcardList[i])[2]; }//Because record is found in 3rd column.
                        break;
                    }
                    i++;
                }
            }
            return record;
        }

        private string ReadFirstLine(string textfile)
        {
            string permission = File.ReadLines(textfile).First();
            return permission;
        }

        private bool CheckTimeStamp(string fileName, int hoursRange)
        {

            DateTime modificationTime = File.GetLastWriteTime(fileName);
            DateTime currentTime = DateTime.Now;
            bool sameDate = false;
            bool withinRange = false;

            int modDate = modificationTime.Day;
            int currentDate = currentTime.Day;
            if (modDate == currentDate) { sameDate = true; }

            int modHour = modificationTime.Hour;
            int currentHour = currentTime.Hour;
            if ((currentHour - modHour) <= hoursRange) { withinRange = true; }

            if ((withinRange == true) && (sameDate == true))
            {
                return true;
            }
            else { return false; }
        }

        public void PollTxtFile()//Checks the most recent .txt file update in QuestionLog folder. Handles null exceptions.
        {

            string Username = Environment.UserName;
            bool newFile = false;
            bool changedFile = false;

            FileSystemWatcher fileWatcher = new FileSystemWatcher();
            fileWatcher.Path = @"C:\nzhs\questioninformation\QuestionLog\";
            fileWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                        | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            fileWatcher.Filter = "*.txt";

            while (true)
            {
                Thread.Sleep(100);

                CheckChrome();

                MethodInvoker AwaitTextChange = delegate ()
                {

                    //Event handling for given filewatcher filters. Allows detection of file update/creation and
                    //modification detection of POST info sent by bluetooth link.
                    fileWatcher.EnableRaisingEvents = true;
                    fileWatcher.Created += delegate { newFile = true; };
                    fileWatcher.Changed += delegate { changedFile = true; };

                    if (newFile == true || changedFile == true)//Un-comment else for user-interactivity
                    {
                        //Below only occurrs on the event of a .txt file update or creation withing QuestionLog folder.
                        //Open a dictionary which contains the relevant bookmark for each question.

                        latestFile = getLatest(@"C:\nzhs\questioninformation\QuestionLog\");

                        System.Threading.Thread.Sleep(300);
                        Thread recordThread = new Thread(new ThreadStart(RecordInSecret));//Operation must run on separate thread.
                        record = ShouldYouRecord(latestFile);
                        recordThread.Start();

                        //Reset booleans so they may be deemed true upon a new update/creation i.e. loop->if iteration.
                        changedFile = false;
                        newFile = false;
                    }

                };
                try { this.Invoke(AwaitTextChange); }
                catch (ObjectDisposedException e) { } //Cannot perform form control if form is closed. This catches that exception.
            }

        }

        private string getLatest(string directory)//Gets the name of the latest file created/updated in QuestionLog directory.
        {
            string Username = Environment.UserName;
            DirectoryInfo questionDirectory = new DirectoryInfo(directory);
            string latestFile = Path.GetFileNameWithoutExtension(FindLatestFile(questionDirectory).Name);
            return latestFile;

        }

        private static FileInfo FindLatestFile(DirectoryInfo directoryInfo)//Gets file info of latest file updated/created in directory.
        {
            if (directoryInfo == null || !directoryInfo.Exists)
                return null;

            FileInfo[] files = directoryInfo.GetFiles();
            DateTime lastWrite = DateTime.MinValue;
            FileInfo lastWrittenFile = null;

            foreach (FileInfo file in files)
            {
                if (file.LastWriteTime > lastWrite)
                {
                    lastWrite = file.LastWriteTime;
                    lastWrittenFile = file;
                }
            }
            return lastWrittenFile;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Opacity = 0;
            Thread audioRecordingThread = new Thread(new ThreadStart(PollTxtFile));//Operation must run on separate thread.
            audioRecordingThread.Start();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            Visible = false;
            Opacity = 100;
        }

        private void CheckChrome()
        {
            if (Process.GetProcessesByName("chrome").Length == 0)
            {
                foreach (Process proc in Process.GetProcessesByName("SurveyAudioRecording"))
                {
                    proc.Kill();
                }
            }
        }
    }
}
