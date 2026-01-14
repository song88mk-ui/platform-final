using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Operator.Cashout.Service.Repository;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using RabbitMQ.Client.Events;

namespace Platform.Operator.Cashout.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMessageBroker _messageBroker;
        private readonly IBettingRepository _bettingRepository;

        private Thread _thread0;
        private Thread _thread1;
        private Thread _thread2;
        private Thread _thread3;
        private Thread _thread4;
        private Thread _thread5;
        private Thread _thread6;
        private Thread _thread7;
        private Thread _thread8;
        private Thread _thread9;

        private Thread _threadA;
        private Thread _threadB;
        private Thread _threadC;
        private Thread _threadD;
        private Thread _threadE;
        private Thread _threadF;
        private Thread _threadG;
        private Thread _threadH;
        private Thread _threadI;
        private Thread _threadJ;
        private Thread _threadK;
        private Thread _threadL;
        private Thread _threadM;
        private Thread _threadN;
        private Thread _threadO;
        private Thread _threadP;
        private Thread _threadQ;
        private Thread _threadR;
        private Thread _threadS;
        private Thread _threadT;
        private Thread _threadU;
        private Thread _threadV;
        private Thread _threadW;
        private Thread _threadX;
        private Thread _threadY;
        private Thread _threadZ;

        private readonly BlockingCollection<BasicDeliverEventArgs> _messages0 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages1 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages2 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages3 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages4 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages5 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages6 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages7 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages8 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages9 = new BlockingCollection<BasicDeliverEventArgs>();

        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesA = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesB = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesC = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesD = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesE = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesF = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesG = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesH = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesI = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesJ = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesK = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesL = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesM = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesN = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesO = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesP = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesQ = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesR = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesS = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesT = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesU = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesV = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesW = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesX = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesY = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messagesZ = new BlockingCollection<BasicDeliverEventArgs>();

        public Worker(ILogger<Worker> logger, IMessageBroker messageBroker, IBettingRepository bettingRepository)
        {
            _logger = logger;
            _messageBroker = messageBroker;
            _bettingRepository = bettingRepository;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _messageBroker.Dispose();

            return base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Preparing thread pool");
            _thread0 = new Thread(ThreadStart0);
            _thread1 = new Thread(ThreadStart1);
            _thread2 = new Thread(ThreadStart2);
            _thread3 = new Thread(ThreadStart3);
            _thread4 = new Thread(ThreadStart4);
            _thread5 = new Thread(ThreadStart5);
            _thread6 = new Thread(ThreadStart6);
            _thread7 = new Thread(ThreadStart7);
            _thread8 = new Thread(ThreadStart8);
            _thread9 = new Thread(ThreadStart9);

            _threadA = new Thread(ThreadStartA);
            _threadB = new Thread(ThreadStartB);
            _threadC = new Thread(ThreadStartC);
            _threadD = new Thread(ThreadStartD);
            _threadE = new Thread(ThreadStartE);
            _threadF = new Thread(ThreadStartF);
            _threadG = new Thread(ThreadStartG);
            _threadH = new Thread(ThreadStartH);
            _threadI = new Thread(ThreadStartI);
            _threadJ = new Thread(ThreadStartJ);

            _threadK = new Thread(ThreadStartK);
            _threadL = new Thread(ThreadStartL);
            _threadM = new Thread(ThreadStartM);
            _threadN = new Thread(ThreadStartN);
            _threadO = new Thread(ThreadStartO);
            _threadP = new Thread(ThreadStartP);
            _threadQ = new Thread(ThreadStartQ);
            _threadR = new Thread(ThreadStartR);
            _threadS = new Thread(ThreadStartS);
            _threadT = new Thread(ThreadStartT);

            _threadU = new Thread(ThreadStartU);
            _threadV = new Thread(ThreadStartV);
            _threadW = new Thread(ThreadStartW);
            _threadX = new Thread(ThreadStartX);
            _threadY = new Thread(ThreadStartY);
            _threadZ = new Thread(ThreadStartZ);

            _logger.LogInformation("Starting thread pool");

            _thread0.Start();
            _thread1.Start();
            _thread2.Start();
            _thread3.Start();
            _thread4.Start();
            _thread5.Start();
            _thread6.Start();
            _thread7.Start();
            _thread8.Start();
            _thread9.Start();

            _threadA.Start();
            _threadB.Start();
            _threadC.Start();
            _threadD.Start();
            _threadE.Start();
            _threadF.Start();
            _threadG.Start();
            _threadH.Start();
            _threadI.Start();
            _threadJ.Start();
            _threadK.Start();
            _threadL.Start();
            _threadM.Start();
            _threadN.Start();
            _threadO.Start();
            _threadP.Start();
            _threadQ.Start();
            _threadR.Start();
            _threadS.Start();
            _threadT.Start();
            _threadU.Start();
            _threadV.Start();
            _threadW.Start();
            _threadX.Start();
            _threadY.Start();
            _threadZ.Start();

            _logger.LogInformation("Connecting to broker/RabbitMQ");
            _messageBroker.ConnectWithManualAck();

            _logger.LogInformation("Connected. Processing incoming messages");
            _messageBroker.MessageReceived += MessageReceivedEvent;

            return Task.CompletedTask;
        }

        private void MessageReceivedEvent(object sender, BasicDeliverEventArgs e)
        {
            var cashout = Encoding.UTF8.GetString(e.Body);
            var messageType = e.BasicProperties.Type;

            var firstChar = string.Empty;

            try
            {
                switch (messageType)
                {
                    case "OnBetCashOutResult":
                        var onBetCashOutResult = JsonSerializer.Deserialize<OnCashOutResult>(cashout);
                        firstChar = onBetCashOutResult.BetId.ToUpper().Substring(0, 1);
                        break;
                }

                switch (firstChar)
                {
                    case "0":
                        _messages0.Add(e);
                        break;
                    case "1":
                        _messages1.Add(e);
                        break;
                    case "2":
                        _messages2.Add(e);
                        break;
                    case "3":
                        _messages3.Add(e);
                        break;
                    case "4":
                        _messages4.Add(e);
                        break;
                    case "5":
                        _messages5.Add(e);
                        break;
                    case "6":
                        _messages6.Add(e);
                        break;
                    case "7":
                        _messages7.Add(e);
                        break;
                    case "8":
                        _messages8.Add(e);
                        break;
                    case "9":
                        _messages9.Add(e);
                        break;

                    case "A":
                        _messagesA.Add(e);
                        break;
                    case "B":
                        _messagesB.Add(e);
                        break;
                    case "C":
                        _messagesC.Add(e);
                        break;
                    case "D":
                        _messagesD.Add(e);
                        break;
                    case "E":
                        _messagesE.Add(e);
                        break;
                    case "F":
                        _messagesF.Add(e);
                        break;
                    case "G":
                        _messagesG.Add(e);
                        break;
                    case "H":
                        _messagesH.Add(e);
                        break;
                    case "I":
                        _messagesI.Add(e);
                        break;
                    case "J":
                        _messagesJ.Add(e);
                        break;

                    case "K":
                        _messagesK.Add(e);
                        break;
                    case "L":
                        _messagesL.Add(e);
                        break;
                    case "M":
                        _messagesM.Add(e);
                        break;
                    case "N":
                        _messagesN.Add(e);
                        break;
                    case "O":
                        _messagesO.Add(e);
                        break;
                    case "P":
                        _messagesP.Add(e);
                        break;
                    case "Q":
                        _messagesQ.Add(e);
                        break;
                    case "R":
                        _messagesR.Add(e);
                        break;
                    case "S":
                        _messagesS.Add(e);
                        break;
                    case "T":
                        _messagesT.Add(e);
                        break;

                    case "U":
                        _messagesU.Add(e);
                        break;
                    case "V":
                        _messagesV.Add(e);
                        break;
                    case "W":
                        _messagesW.Add(e);
                        break;
                    case "X":
                        _messagesX.Add(e);
                        break;
                    case "Y":
                        _messagesY.Add(e);
                        break;
                    case "Z":
                        _messagesZ.Add(e);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error on receiving message");
                _messageBroker.Acknowledge(e);
            }
        }

        private void ProcessMessage(BasicDeliverEventArgs e)
        {
            var cashout = Encoding.UTF8.GetString(e.Body);
            var messageType = e.BasicProperties.Type;
            var correlationId = e.BasicProperties.CorrelationId;

            try
            {
                switch (messageType)
                {
                    case "OnBetCashOutResult":
                        var onBetCashOutResult = JsonSerializer.Deserialize<OnCashOutResult>(cashout);
                        _logger.LogInformation($"{DateTime.UtcNow} {onBetCashOutResult.BetId} - OnBetCashOutResult");

                        if (onBetCashOutResult.BetStatus != Shared.Enums.BetStatus.CashedOut)
                            _bettingRepository.ProcessCashOutResult(onBetCashOutResult, _messageBroker, correlationId);
                        break;
                }

                _messageBroker.Acknowledge(e);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                _messageBroker.Acknowledge(e);
            }
        }

        private void ThreadStart0()
        {
            while (true)
            {
                var message = _messages0.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStart1()
        {
            while (true)
            {
                var message = _messages1.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStart2()
        {
            while (true)
            {
                var message = _messages2.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStart3()
        {
            while (true)
            {
                var message = _messages3.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStart4()
        {
            while (true)
            {
                var message = _messages4.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStart5()
        {
            while (true)
            {
                var message = _messages5.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStart6()
        {
            while (true)
            {
                var message = _messages6.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStart7()
        {
            while (true)
            {
                var message = _messages7.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStart8()
        {
            while (true)
            {
                var message = _messages8.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStart9()
        {
            while (true)
            {
                var message = _messages9.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartA()
        {
            while (true)
            {
                var message = _messagesA.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartB()
        {
            while (true)
            {
                var message = _messagesB.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartC()
        {
            while (true)
            {
                var message = _messagesC.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartD()
        {
            while (true)
            {
                var message = _messagesD.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartE()
        {
            while (true)
            {
                var message = _messagesE.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartF()
        {
            while (true)
            {
                var message = _messagesF.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartG()
        {
            while (true)
            {
                var message = _messagesG.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartH()
        {
            while (true)
            {
                var message = _messagesH.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartI()
        {
            while (true)
            {
                var message = _messagesI.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartJ()
        {
            while (true)
            {
                var message = _messagesJ.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartK()
        {
            while (true)
            {
                var message = _messagesK.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartL()
        {
            while (true)
            {
                var message = _messagesL.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartM()
        {
            while (true)
            {
                var message = _messagesM.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartN()
        {
            while (true)
            {
                var message = _messagesN.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartO()
        {
            while (true)
            {
                var message = _messagesO.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartP()
        {
            while (true)
            {
                var message = _messagesP.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartQ()
        {
            while (true)
            {
                var message = _messagesQ.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartR()
        {
            while (true)
            {
                var message = _messagesR.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartS()
        {
            while (true)
            {
                var message = _messagesS.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartT()
        {
            while (true)
            {
                var message = _messagesT.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartU()
        {
            while (true)
            {
                var message = _messagesU.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartV()
        {
            while (true)
            {
                var message = _messagesV.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartW()
        {
            while (true)
            {
                var message = _messagesW.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartX()
        {
            while (true)
            {
                var message = _messagesX.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartY()
        {
            while (true)
            {
                var message = _messagesY.Take();
                ProcessMessage(message);
            }
        }

        private void ThreadStartZ()
        {
            while (true)
            {
                var message = _messagesZ.Take();
                ProcessMessage(message);
            }
        }
    }
}
