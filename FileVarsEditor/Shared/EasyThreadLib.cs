using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Libs
{
    /*
     Developed by Rafael Tonello at 2017-03
     E-mail: tonello.rafinha@gmail.com
    */

    public class EasyThread
    {
        /// <summary>
        /// List of instantiated (EasyThreads).
        /// </summary>
        public static List<EasyThread> threadList = new List<EasyThread>();
        /// <summary>
        /// Thread states:
        ///     noInit: Thread not started
        ///     running: Thread is running
        ///     exited: Thread has finish executing
        /// </summary>
        public enum ThreadStatus { noInit, running, exited}

        /// <summary>
        /// Description of the function performed by the thread
        /// </summary>
        /// <param name="sender">Current EasyThread (Sender)</param>
        /// <param name="parameters">Parameters to be sent to the function</param>
        public delegate void EasyThreadFun(EasyThread sender, object parameters);

        private bool _running = true;
        private ThreadStatus __status = ThreadStatus.noInit;
        private bool __pause = false;

        private Thread thread;

        /// <summary>
        /// Default contructor. You will nedd to class the "Start" method to start th ethread
        /// </summary>
        public EasyThread()
        {
            EasyThread.threadList.Add(this);
        }

        /// <summary>
        /// Construtor que já inicializa as operações da thread.
        /// </summary>
        /// <param name="fun">Função a ser executada</param>
        /// <param name="runAsWhileTrue">Indica se a thread deve chamar a função "fun" em um "while true". Caso seja true, a função é executada repetidamente.</param>
        /// <param name="thParams">Parametros que serão passados para a função "fun"</param>
        public EasyThread(EasyThreadFun fun, bool runAsWhileTrue, object thParams = null)
        {
            EasyThread.threadList.Add(this);
            this.Start(fun, runAsWhileTrue, thParams);
        }

        /// <summary>
        /// Função que inicializa as operações da thread. Quando o contrutor EasyThread(EasyThreadFun, bool, object) é utilizado, esta função é executada automaticamente.
        /// </summary>
        /// <param name="fun">Função a ser executada</param>
        /// <param name="runAsWhileTrue">Indica se a thread deve chamar a função "fun" em um "while true". Caso seja true, a função é executada repetidamente.</param>
        /// <param name="thParams">Parametros que serão passados para a função "fun"</param>
        public void Start(EasyThreadFun fun, bool runAsWhileTrue, object thParams = null)
        {

            if (this.canRun())
            {
                thread = new Thread(delegate ()
                {
                    this.__status = ThreadStatus.running;
                    if (runAsWhileTrue)
                    {
                        while (this.canRun())
                        {
                            if (!__pause)
                            {
                                fun(this, thParams);
                            }
                            else
                            {
                                this.sleep(1);
                            }
                        }
                        this.__status = ThreadStatus.exited;
                    }
                    else
                    {
                        fun(this, thParams);
                        this.__status = ThreadStatus.exited;
                    }
                });
                thread.Start();
            }
        }

        /// <summary>
        /// Pára a execução da thread por um intervalo. O comando é redirecionado para a função System.Threading.Thread.Sleep
        /// </summary>
        /// <param name="sleepMs">Intervalo, em milissegundos, para parar</param>
        public void sleep(int sleepMs)
        {
            Thread.Sleep(sleepMs);
        }

        /// <summary>
        /// Returna true quando a thread pode ser executada. Quando a função stopThread é executada, esta função começará a retornar sempre "false", indicando
        /// que a thread não pode mais ser executada. Deve ser utilizada quando a thread é inicializada no modo não "runAsWhileTrue", e se utilizada um "while"
        /// na função "fun"
        /// </summary>
        /// <returns></returns>
        public bool canRun()
        {
            return _running;
        }

        /// <summary>
        /// Retorna o status da Thread.
        /// </summary>
        /// <returns></returns>
        public ThreadStatus getThreadStatus()
        {
            return this.__status;

        }

        public void pause()
        {
            __pause = true;
            
        }

        public void resume()
        {
            __pause = false;
        }

        public bool isPaused()
        {
            return __pause;
        }

        /// <summary>
        /// Finaliza as operações da thread
        /// </summary>
        /// <param name="awaitStop">Aguarda a finalização da Thread.</param>
        public void stop(bool awaitStop = false)
        {
            _running = false;

            if (awaitStop)
            {
                while (this.getThreadStatus() == ThreadStatus.running)
                    Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Pára todas as EasyThreads
        /// </summary>
        /// <param name="await">Aguarda a finalização de cada thread</param>
        public static void stopAllThreads(bool await)
        {
            for (int cont = 0; cont < EasyThread.threadList.Count; cont++)
                if (EasyThread.threadList[cont] != null)
                    EasyThread.threadList[cont].stop(await);
        }

        /// <summary>
        /// Cria um novo objeto EasyThread
        /// </summary>
        /// <param name="fun">Função a ser execuata</param>
        /// <param name="runAsWhileTrue">Indica se deve executar como um while true</param>
        /// <param name="thParams">Parametros que serão passados para a thread</param>
        /// <returns></returns>
        public static EasyThread StartNew(EasyThreadFun fun, bool runAsWhileTrue, object thParams = null)
        {
            return new EasyThread(fun, runAsWhileTrue, thParams);
        }

    }
}
