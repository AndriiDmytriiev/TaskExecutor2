using Task = System.Threading.Tasks.Task;
using Thread = System.Threading.Thread;
using Barrier = System.Threading.Barrier;
using Monitor = System.Threading.Monitor;
using IDisposable = System.IDisposable;
using TaskEnum = System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task>;
using TaskQueue = System.Collections.Generic.Queue<System.Threading.Tasks.Task>;
using Enumerable = System.Linq.Enumerable;
using ObjectDisposedException = System.ObjectDisposedException;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using _Imported_Extensions_;
using System.Data;
using System.Globalization;
using Npgsql;
using static System.Net.Mime.MediaTypeNames;

namespace _Imported_Extensions_
{

    public static class Extensions
    {
        public static bool Any(this TaskEnum te)
        {
            return Enumerable.Any(te);
        }
        
         public static TaskEnum ToList(this TaskEnum te)
        {
            return Enumerable.ToList(te);
        }
    }
}

namespace TaskUtils
{
    public class SameThreadTaskScheduler : System.Threading.Tasks.TaskScheduler, IDisposable
    {
        public static string strDateString = "";
        static void Main(string[] args)
        {

            /*var TaskScheduler = new SameThreadTaskScheduler("RunIt");
            TaskScheduler.StartThread("");
            TaskScheduler.GetScheduledTasks();*/
            // Create a scheduler that uses two threads. 
            
            List<Task> tasks = new List<Task>();

            // Create a TaskFactory and pass it our custom scheduler. 
            

            // Use our factory to run a set of tasks. 
            Object lockObj = new Object();
            int outputItem = 0;

            for (int tCtr = 0; tCtr <= 4; tCtr++)
            {
                int iteration = tCtr;
                Task t = Task.Run(() => {
                    for (int i = 0; i < 1000; i++)
                    {
                        lock (lockObj)
                        {
                            doStuff("Task"+i.ToString());
                            Console.Write("{0} in task t-{1} on thread {2}   ",
                                          i, iteration, Thread.CurrentThread.ManagedThreadId);
                            outputItem++;
                            if (outputItem % 3 == 0)
                                Console.WriteLine();
                        }
                    }
                });



                tasks.Add(t);
            }
            // Use it to run a second set of tasks.                       
            //for (int tCtr = 0; tCtr <= 4; tCtr++)
            //{
            //    int iteration = tCtr;
            //    Task t1 = Task.Run(() => {
            //        for (int outer = 0; outer <= 10; outer++)
            //        {
            //            for (int i = 0; i <= 1; i++)
            //            {
            //                lock (lockObj)
            //                {
            //                    doStuff("Task" + i.ToString());
            //                    Console.Write("'{0}' in task t1-{1} on thread {2}   ",
            //                                  Convert.ToChar(i), iteration, Thread.CurrentThread.ManagedThreadId);
            //                    outputItem++;
            //                    if (outputItem % 3 == 0)
            //                        Console.WriteLine();
            //                }
            //            }
            //        }
            //    });
            //    tasks.Add(t1);
            //}

            // Wait for the tasks to complete before displaying a completion message.

            //Task.WaitAll(tasks.ToArray());

            ExecuteJob(tasks.ToArray());
            
            Console.WriteLine("\n\nSuccessful completion.");
            Console.ReadKey();
           
        }
        private static async void ExecuteJob(Task[] test)
        {

            await Task.WhenAll(test).ConfigureAwait(false); 
        }

        #region publics
        public SameThreadTaskScheduler(string name)
        {
            scheduledTasks = new TaskQueue();
            threadName = name;
        }
        
        public override int MaximumConcurrencyLevel { get { return 1; } }
        public void Dispose()
        {
            lock (scheduledTasks)
            {
                quit = true;
                Monitor.PulseAll(scheduledTasks);
            }
        }
        #endregion

        #region protected overrides
        protected override TaskEnum GetScheduledTasks()
        {
            lock (scheduledTasks)
            {
                return scheduledTasks.ToList();
            }
        }
        protected override void QueueTask(Task task)
        {
            if (myThread == null)
                myThread = StartThread(threadName);
            if (!myThread.IsAlive)
                throw new ObjectDisposedException("My thread is not alive, so this object has been disposed!");
            lock (scheduledTasks)
            {
                scheduledTasks.Enqueue(task);
                Monitor.PulseAll(scheduledTasks);
            }
        }
        protected override bool TryExecuteTaskInline(Task task, bool task_was_previously_queued)
        {
            return false;
        }
        #endregion

        private readonly TaskQueue scheduledTasks;
        private Thread myThread;
        private readonly string threadName;
      
        private bool quit;
        public static string shortDateString = "ddd";

        private Thread StartThread(string name)
        {
            var t = new Thread(MyThread) { Name = name };
            using (var start = new Barrier(2))
            {
                t.Start(start);
                ReachBarrier(start);
            }
            return t;
        }
        private void MyThread(object o)
        {
            Task tsk;
            lock (scheduledTasks)
            {
                //When reaches the barrier, we know it holds the lock.
                //
                //So there is no Pulse call can trigger until
                //this thread starts to wait for signals.
                //
                //It is important not to call StartThread within a lock.
                //Otherwise, deadlock!
                ReachBarrier(o as Barrier);
                tsk = WaitAndDequeueTask();
            }
            for (;;)
            {
                if (tsk == null)
                    break;
                TryExecuteTask(tsk);
                lock (scheduledTasks)
                {
                    tsk = WaitAndDequeueTask();
                }
            }
        }
        private Task WaitAndDequeueTask()
        {
            while (!scheduledTasks.Any() && !quit)
                Monitor.Wait(scheduledTasks);
            return quit ? null : scheduledTasks.Dequeue();
        }

        private static void ReachBarrier(Barrier b)
        {
            if (b != null)
                b.SignalAndWait();
        }
        private static NpgsqlConnection GetConnection()
        {
            var connection = System.Configuration.ConfigurationManager.ConnectionStrings["ConnStr"].ConnectionString;
            string ConnStr = connection;
            return new NpgsqlConnection(ConnStr);

        }
        public static void doStuff(string strName)
        {

            using (NpgsqlConnection conn = GetConnection())
            {
                conn.Open();
                if (conn.State == ConnectionState.Open)
                {/* label1.Text = "Connected";*/ }
                var cmd2 = new NpgsqlCommand("select * from Employees where employeeid=1", conn);
                var da = new NpgsqlDataAdapter(cmd2);
                var ds = new DataSet();
                NpgsqlDataReader dr = cmd2.ExecuteReader();

                dr.Close();
                conn.Close();

            }

            using (NpgsqlConnection conn = GetConnection())
            {
                conn.Open();
               
                var currentCulture = Thread.CurrentThread.CurrentCulture;
                try
                {
                    Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-us");
                    shortDateString = DateTime.Now.ToShortDateString();
                    var strTime = DateTime.Now.ToLongTimeString();
                    var strDateString = shortDateString.Replace("/ ", "-");

                    // Do something with shortDateString...
                }
                finally
                {
                    Thread.CurrentThread.CurrentCulture = currentCulture;
                }

                var dtNow = new DateTime();
                string TaskID = "";

                Random rnd = new Random();
                
                int rndNum = rnd.Next(1000000);

               
                var strSQL = @"insert into TaskResults(TaskID, TaskName, TaskDate) values('"; 
                strSQL += rndNum.ToString() + "','" + strName.ToString();
                strSQL += "','" +shortDateString + "'" + ")";

                var cmd2 = new NpgsqlCommand(strSQL, conn);
                int nNoAdded = cmd2.ExecuteNonQuery();

                conn.Close();

            }


            //MessageBox.Show(strName);

            Thread.Yield();

        }
    }
}



    
        
