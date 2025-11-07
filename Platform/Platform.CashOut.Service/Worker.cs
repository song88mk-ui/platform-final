using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.CashOut.Service.Repository;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using RabbitMQ.Client.Events;
using Sportradar.MTS.SDK.API;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.CashOut.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMessageBroker _messageBroker;
        private readonly IBettingRepository _bettingRepository;
        private readonly IConfiguration _configuration;
        private IMtsSdk _mts;

        private Stack<int> _index = new Stack<int>();

        private Thread _thread00;
        private Thread _thread01;
        private Thread _thread02;
        private Thread _thread03;
        private Thread _thread04;
        private Thread _thread05;
        private Thread _thread06;
        private Thread _thread07;
        private Thread _thread08;
        private Thread _thread09;
        private Thread _thread10;
        private Thread _thread11;
        private Thread _thread12;
        private Thread _thread13;
        private Thread _thread14;
        private Thread _thread15;
        private Thread _thread16;
        private Thread _thread17;
        private Thread _thread18;
        private Thread _thread19;
        private Thread _thread20;
        private Thread _thread21;
        private Thread _thread22;
        private Thread _thread23;
        private Thread _thread24;
        private Thread _thread25;
        private Thread _thread26;
        private Thread _thread27;
        private Thread _thread28;
        private Thread _thread29;
        private Thread _thread30;
        private Thread _thread31;
        private Thread _thread32;
        private Thread _thread33;
        private Thread _thread34;
        private Thread _thread35;
        private Thread _thread36;
        private Thread _thread37;
        private Thread _thread38;
        private Thread _thread39;
        private Thread _thread40;
        private Thread _thread41;
        private Thread _thread42;
        private Thread _thread43;
        private Thread _thread44;
        private Thread _thread45;
        private Thread _thread46;
        private Thread _thread47;
        private Thread _thread48;
        private Thread _thread49;
        private Thread _thread50;
        private Thread _thread51;
        private Thread _thread52;
        private Thread _thread53;
        private Thread _thread54;
        private Thread _thread55;
        private Thread _thread56;
        private Thread _thread57;
        private Thread _thread58;
        private Thread _thread59;
        private Thread _thread60;
        private Thread _thread61;
        private Thread _thread62;
        private Thread _thread63;
        private Thread _thread64;
        private Thread _thread65;
        private Thread _thread66;
        private Thread _thread67;
        private Thread _thread68;
        private Thread _thread69;
        private Thread _thread70;
        private Thread _thread71;
        private Thread _thread72;
        private Thread _thread73;
        private Thread _thread74;
        private Thread _thread75;
        private Thread _thread76;
        private Thread _thread77;
        private Thread _thread78;
        private Thread _thread79;
        private Thread _thread80;
        private Thread _thread81;
        private Thread _thread82;
        private Thread _thread83;
        private Thread _thread84;
        private Thread _thread85;
        private Thread _thread86;
        private Thread _thread87;
        private Thread _thread88;
        private Thread _thread89;
        private Thread _thread90;
        private Thread _thread91;
        private Thread _thread92;
        private Thread _thread93;
        private Thread _thread94;
        private Thread _thread95;
        private Thread _thread96;
        private Thread _thread97;
        private Thread _thread98;
        private Thread _thread99;

        private readonly BlockingCollection<BasicDeliverEventArgs> _messages00 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages01 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages02 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages03 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages04 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages05 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages06 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages07 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages08 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages09 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages10 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages11 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages12 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages13 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages14 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages15 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages16 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages17 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages18 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages19 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages20 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages21 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages22 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages23 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages24 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages25 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages26 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages27 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages28 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages29 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages30 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages31 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages32 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages33 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages34 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages35 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages36 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages37 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages38 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages39 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages40 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages41 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages42 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages43 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages44 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages45 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages46 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages47 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages48 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages49 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages50 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages51 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages52 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages53 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages54 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages55 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages56 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages57 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages58 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages59 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages60 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages61 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages62 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages63 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages64 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages65 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages66 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages67 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages68 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages69 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages70 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages71 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages72 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages73 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages74 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages75 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages76 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages77 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages78 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages79 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages80 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages81 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages82 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages83 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages84 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages85 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages86 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages87 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages88 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages89 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages90 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages91 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages92 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages93 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages94 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages95 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages96 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages97 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages98 = new BlockingCollection<BasicDeliverEventArgs>();
        private readonly BlockingCollection<BasicDeliverEventArgs> _messages99 = new BlockingCollection<BasicDeliverEventArgs>();

        public Worker(ILogger<Worker> logger, IMessageBroker messageBroker, IBettingRepository bettingRepository, IConfiguration configuration)
        {
            _logger = logger;
            _messageBroker = messageBroker;
            _bettingRepository = bettingRepository;
            _configuration = configuration;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            if (Convert.ToBoolean(_configuration["Custom:IsMTSEnabled"]))
            {
                try
                {
                    var config = MtsSdk.GetConfiguration();

                    _mts = new MtsSdk(config);
                    _mts.Open();
                    _logger.LogInformation("The application has started");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while starting the application");
                }
            }

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            if (Convert.ToBoolean(_configuration["Custom:IsMTSEnabled"]))
            {
                try
                {
                    _mts.Close();

                    _messageBroker.Dispose();
                    _logger.LogInformation("The application has stopped");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while stopping the application");
                }
            }

            _messageBroker.Dispose();

            return base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _thread00 = new Thread(ThreadStart00);
            _thread01 = new Thread(ThreadStart01);
            _thread02 = new Thread(ThreadStart02);
            _thread03 = new Thread(ThreadStart03);
            _thread04 = new Thread(ThreadStart04);
            _thread05 = new Thread(ThreadStart05);
            _thread06 = new Thread(ThreadStart06);
            _thread07 = new Thread(ThreadStart07);
            _thread08 = new Thread(ThreadStart08);
            _thread09 = new Thread(ThreadStart09);

            _thread10 = new Thread(ThreadStart10);
            _thread11 = new Thread(ThreadStart11);
            _thread12 = new Thread(ThreadStart12);
            _thread13 = new Thread(ThreadStart13);
            _thread14 = new Thread(ThreadStart14);
            _thread15 = new Thread(ThreadStart15);
            _thread16 = new Thread(ThreadStart16);
            _thread17 = new Thread(ThreadStart17);
            _thread18 = new Thread(ThreadStart18);
            _thread19 = new Thread(ThreadStart19);

            _thread20 = new Thread(ThreadStart20);
            _thread21 = new Thread(ThreadStart21);
            _thread22 = new Thread(ThreadStart22);
            _thread23 = new Thread(ThreadStart23);
            _thread24 = new Thread(ThreadStart24);
            _thread25 = new Thread(ThreadStart25);
            _thread26 = new Thread(ThreadStart26);
            _thread27 = new Thread(ThreadStart27);
            _thread28 = new Thread(ThreadStart28);
            _thread29 = new Thread(ThreadStart29);

            _thread30 = new Thread(ThreadStart30);
            _thread31 = new Thread(ThreadStart31);
            _thread32 = new Thread(ThreadStart32);
            _thread33 = new Thread(ThreadStart33);
            _thread34 = new Thread(ThreadStart34);
            _thread35 = new Thread(ThreadStart35);
            _thread36 = new Thread(ThreadStart36);
            _thread37 = new Thread(ThreadStart37);
            _thread38 = new Thread(ThreadStart38);
            _thread39 = new Thread(ThreadStart39);

            _thread40 = new Thread(ThreadStart40);
            _thread41 = new Thread(ThreadStart41);
            _thread42 = new Thread(ThreadStart42);
            _thread43 = new Thread(ThreadStart43);
            _thread44 = new Thread(ThreadStart44);
            _thread45 = new Thread(ThreadStart45);
            _thread46 = new Thread(ThreadStart46);
            _thread47 = new Thread(ThreadStart47);
            _thread48 = new Thread(ThreadStart48);
            _thread49 = new Thread(ThreadStart49);

            _thread50 = new Thread(ThreadStart50);
            _thread51 = new Thread(ThreadStart51);
            _thread52 = new Thread(ThreadStart52);
            _thread53 = new Thread(ThreadStart53);
            _thread54 = new Thread(ThreadStart54);
            _thread55 = new Thread(ThreadStart55);
            _thread56 = new Thread(ThreadStart56);
            _thread57 = new Thread(ThreadStart57);
            _thread58 = new Thread(ThreadStart58);
            _thread59 = new Thread(ThreadStart59);

            _thread60 = new Thread(ThreadStart60);
            _thread61 = new Thread(ThreadStart61);
            _thread62 = new Thread(ThreadStart62);
            _thread63 = new Thread(ThreadStart63);
            _thread64 = new Thread(ThreadStart64);
            _thread65 = new Thread(ThreadStart65);
            _thread66 = new Thread(ThreadStart66);
            _thread67 = new Thread(ThreadStart67);
            _thread68 = new Thread(ThreadStart68);
            _thread69 = new Thread(ThreadStart69);

            _thread70 = new Thread(ThreadStart70);
            _thread71 = new Thread(ThreadStart71);
            _thread72 = new Thread(ThreadStart72);
            _thread73 = new Thread(ThreadStart73);
            _thread74 = new Thread(ThreadStart74);
            _thread75 = new Thread(ThreadStart75);
            _thread76 = new Thread(ThreadStart76);
            _thread77 = new Thread(ThreadStart77);
            _thread78 = new Thread(ThreadStart78);
            _thread79 = new Thread(ThreadStart79);

            _thread80 = new Thread(ThreadStart80);
            _thread81 = new Thread(ThreadStart81);
            _thread82 = new Thread(ThreadStart82);
            _thread83 = new Thread(ThreadStart83);
            _thread84 = new Thread(ThreadStart84);
            _thread85 = new Thread(ThreadStart85);
            _thread86 = new Thread(ThreadStart86);
            _thread87 = new Thread(ThreadStart87);
            _thread88 = new Thread(ThreadStart88);
            _thread89 = new Thread(ThreadStart89);

            _thread90 = new Thread(ThreadStart90);
            _thread91 = new Thread(ThreadStart91);
            _thread92 = new Thread(ThreadStart92);
            _thread93 = new Thread(ThreadStart93);
            _thread94 = new Thread(ThreadStart94);
            _thread95 = new Thread(ThreadStart95);
            _thread96 = new Thread(ThreadStart96);
            _thread97 = new Thread(ThreadStart97);
            _thread98 = new Thread(ThreadStart98);
            _thread99 = new Thread(ThreadStart99);

            _thread00.Start();
            _thread01.Start();
            _thread02.Start();
            _thread03.Start();
            _thread04.Start();
            _thread05.Start();
            _thread06.Start();
            _thread07.Start();
            _thread08.Start();
            _thread09.Start();

            _thread10.Start();
            _thread11.Start();
            _thread12.Start();
            _thread13.Start();
            _thread14.Start();
            _thread15.Start();
            _thread16.Start();
            _thread17.Start();
            _thread18.Start();
            _thread19.Start();

            _thread20.Start();
            _thread21.Start();
            _thread22.Start();
            _thread23.Start();
            _thread24.Start();
            _thread25.Start();
            _thread26.Start();
            _thread27.Start();
            _thread28.Start();
            _thread29.Start();

            _thread30.Start();
            _thread31.Start();
            _thread32.Start();
            _thread33.Start();
            _thread34.Start();
            _thread35.Start();
            _thread36.Start();
            _thread37.Start();
            _thread38.Start();
            _thread39.Start();

            _thread40.Start();
            _thread41.Start();
            _thread42.Start();
            _thread43.Start();
            _thread44.Start();
            _thread45.Start();
            _thread46.Start();
            _thread47.Start();
            _thread48.Start();
            _thread49.Start();

            _thread50.Start();
            _thread51.Start();
            _thread52.Start();
            _thread53.Start();
            _thread54.Start();
            _thread55.Start();
            _thread56.Start();
            _thread57.Start();
            _thread58.Start();
            _thread59.Start();

            _thread60.Start();
            _thread61.Start();
            _thread62.Start();
            _thread63.Start();
            _thread64.Start();
            _thread65.Start();
            _thread66.Start();
            _thread67.Start();
            _thread68.Start();
            _thread69.Start();

            _thread70.Start();
            _thread71.Start();
            _thread72.Start();
            _thread73.Start();
            _thread74.Start();
            _thread75.Start();
            _thread76.Start();
            _thread77.Start();
            _thread78.Start();
            _thread79.Start();

            _thread80.Start();
            _thread81.Start();
            _thread82.Start();
            _thread83.Start();
            _thread84.Start();
            _thread85.Start();
            _thread86.Start();
            _thread87.Start();
            _thread88.Start();
            _thread89.Start();

            _thread90.Start();
            _thread91.Start();
            _thread92.Start();
            _thread93.Start();
            _thread94.Start();
            _thread95.Start();
            _thread96.Start();
            _thread97.Start();
            _thread98.Start();
            _thread99.Start();

            _messageBroker.ConnectWithManualAck();
            _messageBroker.MessageReceived += MessageReceivedEvent;

            return Task.CompletedTask;
        }

        private void MessageReceivedEvent(object sender, BasicDeliverEventArgs e)
        {
            var index = GetIndex();

            switch (index)
            {
                case 0:
                    _messages00.Add(e);
                    break;
                case 1:
                    _messages01.Add(e);
                    break;
                case 2:
                    _messages02.Add(e);
                    break;
                case 3:
                    _messages03.Add(e);
                    break;
                case 4:
                    _messages04.Add(e);
                    break;
                case 5:
                    _messages05.Add(e);
                    break;
                case 6:
                    _messages06.Add(e);
                    break;
                case 7:
                    _messages07.Add(e);
                    break;
                case 8:
                    _messages08.Add(e);
                    break;
                case 9:
                    _messages09.Add(e);
                    break;

                case 10:
                    _messages10.Add(e);
                    break;
                case 11:
                    _messages11.Add(e);
                    break;
                case 12:
                    _messages12.Add(e);
                    break;
                case 13:
                    _messages13.Add(e);
                    break;
                case 14:
                    _messages14.Add(e);
                    break;
                case 15:
                    _messages15.Add(e);
                    break;
                case 16:
                    _messages16.Add(e);
                    break;
                case 17:
                    _messages17.Add(e);
                    break;
                case 18:
                    _messages18.Add(e);
                    break;
                case 19:
                    _messages19.Add(e);
                    break;

                case 20:
                    _messages20.Add(e);
                    break;
                case 21:
                    _messages21.Add(e);
                    break;
                case 22:
                    _messages22.Add(e);
                    break;
                case 23:
                    _messages23.Add(e);
                    break;
                case 24:
                    _messages24.Add(e);
                    break;
                case 25:
                    _messages25.Add(e);
                    break;
                case 26:
                    _messages26.Add(e);
                    break;
                case 27:
                    _messages27.Add(e);
                    break;
                case 28:
                    _messages28.Add(e);
                    break;
                case 29:
                    _messages29.Add(e);
                    break;

                case 30:
                    _messages30.Add(e);
                    break;
                case 31:
                    _messages31.Add(e);
                    break;
                case 32:
                    _messages32.Add(e);
                    break;
                case 33:
                    _messages33.Add(e);
                    break;
                case 34:
                    _messages34.Add(e);
                    break;
                case 35:
                    _messages35.Add(e);
                    break;
                case 36:
                    _messages36.Add(e);
                    break;
                case 37:
                    _messages37.Add(e);
                    break;
                case 38:
                    _messages38.Add(e);
                    break;
                case 39:
                    _messages39.Add(e);
                    break;

                case 40:
                    _messages40.Add(e);
                    break;
                case 41:
                    _messages41.Add(e);
                    break;
                case 42:
                    _messages42.Add(e);
                    break;
                case 43:
                    _messages43.Add(e);
                    break;
                case 44:
                    _messages44.Add(e);
                    break;
                case 45:
                    _messages45.Add(e);
                    break;
                case 46:
                    _messages46.Add(e);
                    break;
                case 47:
                    _messages47.Add(e);
                    break;
                case 48:
                    _messages48.Add(e);
                    break;
                case 49:
                    _messages49.Add(e);
                    break;

                case 50:
                    _messages50.Add(e);
                    break;
                case 51:
                    _messages51.Add(e);
                    break;
                case 52:
                    _messages52.Add(e);
                    break;
                case 53:
                    _messages53.Add(e);
                    break;
                case 54:
                    _messages54.Add(e);
                    break;
                case 55:
                    _messages55.Add(e);
                    break;
                case 56:
                    _messages56.Add(e);
                    break;
                case 57:
                    _messages57.Add(e);
                    break;
                case 58:
                    _messages58.Add(e);
                    break;
                case 59:
                    _messages59.Add(e);
                    break;

                case 60:
                    _messages60.Add(e);
                    break;
                case 61:
                    _messages61.Add(e);
                    break;
                case 62:
                    _messages62.Add(e);
                    break;
                case 63:
                    _messages63.Add(e);
                    break;
                case 64:
                    _messages64.Add(e);
                    break;
                case 65:
                    _messages65.Add(e);
                    break;
                case 66:
                    _messages66.Add(e);
                    break;
                case 67:
                    _messages67.Add(e);
                    break;
                case 68:
                    _messages68.Add(e);
                    break;
                case 69:
                    _messages69.Add(e);
                    break;

                case 70:
                    _messages70.Add(e);
                    break;
                case 71:
                    _messages71.Add(e);
                    break;
                case 72:
                    _messages72.Add(e);
                    break;
                case 73:
                    _messages73.Add(e);
                    break;
                case 74:
                    _messages74.Add(e);
                    break;
                case 75:
                    _messages75.Add(e);
                    break;
                case 76:
                    _messages76.Add(e);
                    break;
                case 77:
                    _messages77.Add(e);
                    break;
                case 78:
                    _messages78.Add(e);
                    break;
                case 79:
                    _messages79.Add(e);
                    break;

                case 80:
                    _messages80.Add(e);
                    break;
                case 81:
                    _messages81.Add(e);
                    break;
                case 82:
                    _messages82.Add(e);
                    break;
                case 83:
                    _messages83.Add(e);
                    break;
                case 84:
                    _messages84.Add(e);
                    break;
                case 85:
                    _messages85.Add(e);
                    break;
                case 86:
                    _messages86.Add(e);
                    break;
                case 87:
                    _messages87.Add(e);
                    break;
                case 88:
                    _messages88.Add(e);
                    break;
                case 89:
                    _messages89.Add(e);
                    break;

                case 90:
                    _messages90.Add(e);
                    break;
                case 91:
                    _messages91.Add(e);
                    break;
                case 92:
                    _messages92.Add(e);
                    break;
                case 93:
                    _messages93.Add(e);
                    break;
                case 94:
                    _messages94.Add(e);
                    break;
                case 95:
                    _messages95.Add(e);
                    break;
                case 96:
                    _messages96.Add(e);
                    break;
                case 97:
                    _messages97.Add(e);
                    break;
                case 98:
                    _messages98.Add(e);
                    break;
                case 99:
                    _messages99.Add(e);
                    break;
            }

        }

        private void ProcessCashOut(BasicDeliverEventArgs e, int threadId)
        {
            var payload = Encoding.UTF8.GetString(e.Body);
            var messageType = e.BasicProperties.Type;
            var correlationId = e.BasicProperties.CorrelationId;

            try
            {
                switch (messageType)
                {
                    case "OnBetFullCashOut":
                        var onCashoutBetSettlement = JsonSerializer.Deserialize<OnCashoutBetSettlement>(payload);
                        _bettingRepository.ProcessCashOut(onCashoutBetSettlement, _mts, _messageBroker, correlationId ?? Guid.NewGuid().ToString());
                        _messageBroker.Acknowledge(e);
                        break;
                        //
                    case "OnBetPartialCashOut":
                        var onPartialCashoutBetSettlement = JsonSerializer.Deserialize<OnCashoutBetSettlement>(payload);
                        _bettingRepository.ProcessCashOut(onPartialCashoutBetSettlement, _mts, _messageBroker, correlationId ?? Guid.NewGuid().ToString());
                        _messageBroker.Acknowledge(e);
                        break;
                    default:
                        _messageBroker.Acknowledge(e);
                        break;
                }
            }
            catch (Exception ex)
            {
                var errorMessage = new
                {
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Payload = payload
                };

                _messageBroker.Publish(errorMessage, "OnBetFullCashOutError", "r.bet.cashout.error", correlationId ?? Guid.NewGuid().ToString());
                _messageBroker.Acknowledge(e);
            }
        }

        private void ThreadStart00()
        {
            while (true)
            {
                var message = _messages00.Take();
                ProcessCashOut(message, 0);
            }
        }

        private void ThreadStart01()
        {
            while (true)
            {
                var message = _messages01.Take();
                ProcessCashOut(message, 1);
            }
        }

        private void ThreadStart02()
        {
            while (true)
            {
                var message = _messages02.Take();
                ProcessCashOut(message, 2);
            }
        }

        private void ThreadStart03()
        {
            while (true)
            {
                var message = _messages03.Take();
                ProcessCashOut(message, 3);
            }
        }

        private void ThreadStart04()
        {
            while (true)
            {
                var message = _messages04.Take();
                ProcessCashOut(message, 4);
            }
        }

        private void ThreadStart05()
        {
            while (true)
            {
                var message = _messages05.Take();
                ProcessCashOut(message, 5);
            }
        }

        private void ThreadStart06()
        {
            while (true)
            {
                var message = _messages06.Take();
                ProcessCashOut(message, 6);
            }
        }

        private void ThreadStart07()
        {
            while (true)
            {
                var message = _messages07.Take();
                ProcessCashOut(message, 7);
            }
        }

        private void ThreadStart08()
        {
            while (true)
            {
                var message = _messages08.Take();
                ProcessCashOut(message, 8);
            }
        }

        private void ThreadStart09()
        {
            while (true)
            {
                var message = _messages09.Take();
                ProcessCashOut(message, 9);
            }
        }


        private void ThreadStart10()
        {
            while (true)
            {
                var message = _messages10.Take();
                ProcessCashOut(message, 10);
            }
        }

        private void ThreadStart11()
        {
            while (true)
            {
                var message = _messages11.Take();
                ProcessCashOut(message, 11);
            }
        }

        private void ThreadStart12()
        {
            while (true)
            {
                var message = _messages12.Take();
                ProcessCashOut(message, 12);
            }
        }

        private void ThreadStart13()
        {
            while (true)
            {
                var message = _messages13.Take();
                ProcessCashOut(message, 13);
            }
        }

        private void ThreadStart14()
        {
            while (true)
            {
                var message = _messages14.Take();
                ProcessCashOut(message, 14);
            }
        }

        private void ThreadStart15()
        {
            while (true)
            {
                var message = _messages15.Take();
                ProcessCashOut(message, 15);
            }
        }

        private void ThreadStart16()
        {
            while (true)
            {
                var message = _messages16.Take();
                ProcessCashOut(message, 16);
            }
        }

        private void ThreadStart17()
        {
            while (true)
            {
                var message = _messages17.Take();
                ProcessCashOut(message, 17);
            }
        }

        private void ThreadStart18()
        {
            while (true)
            {
                var message = _messages18.Take();
                ProcessCashOut(message, 18);
            }
        }

        private void ThreadStart19()
        {
            while (true)
            {
                var message = _messages19.Take();
                ProcessCashOut(message, 19);
            }
        }


        private void ThreadStart20()
        {
            while (true)
            {
                var message = _messages20.Take();
                ProcessCashOut(message, 20);
            }
        }

        private void ThreadStart21()
        {
            while (true)
            {
                var message = _messages21.Take();
                ProcessCashOut(message, 21);
            }
        }

        private void ThreadStart22()
        {
            while (true)
            {
                var message = _messages22.Take();
                ProcessCashOut(message, 22);
            }
        }

        private void ThreadStart23()
        {
            while (true)
            {
                var message = _messages23.Take();
                ProcessCashOut(message, 23);
            }
        }

        private void ThreadStart24()
        {
            while (true)
            {
                var message = _messages24.Take();
                ProcessCashOut(message, 24);
            }
        }

        private void ThreadStart25()
        {
            while (true)
            {
                var message = _messages25.Take();
                ProcessCashOut(message, 25);
            }
        }

        private void ThreadStart26()
        {
            while (true)
            {
                var message = _messages26.Take();
                ProcessCashOut(message, 26);
            }
        }

        private void ThreadStart27()
        {
            while (true)
            {
                var message = _messages27.Take();
                ProcessCashOut(message, 27);
            }
        }

        private void ThreadStart28()
        {
            while (true)
            {
                var message = _messages28.Take();
                ProcessCashOut(message, 28);
            }
        }

        private void ThreadStart29()
        {
            while (true)
            {
                var message = _messages29.Take();
                ProcessCashOut(message, 29);
            }
        }


        private void ThreadStart30()
        {
            while (true)
            {
                var message = _messages30.Take();
                ProcessCashOut(message, 30);
            }
        }

        private void ThreadStart31()
        {
            while (true)
            {
                var message = _messages31.Take();
                ProcessCashOut(message, 31);
            }
        }

        private void ThreadStart32()
        {
            while (true)
            {
                var message = _messages32.Take();
                ProcessCashOut(message, 32);
            }
        }

        private void ThreadStart33()
        {
            while (true)
            {
                var message = _messages33.Take();
                ProcessCashOut(message, 33);
            }
        }

        private void ThreadStart34()
        {
            while (true)
            {
                var message = _messages34.Take();
                ProcessCashOut(message, 34);
            }
        }

        private void ThreadStart35()
        {
            while (true)
            {
                var message = _messages35.Take();
                ProcessCashOut(message, 35);
            }
        }

        private void ThreadStart36()
        {
            while (true)
            {
                var message = _messages36.Take();
                ProcessCashOut(message, 36);
            }
        }

        private void ThreadStart37()
        {
            while (true)
            {
                var message = _messages37.Take();
                ProcessCashOut(message, 37);
            }
        }

        private void ThreadStart38()
        {
            while (true)
            {
                var message = _messages38.Take();
                ProcessCashOut(message, 38);
            }
        }

        private void ThreadStart39()
        {
            while (true)
            {
                var message = _messages39.Take();
                ProcessCashOut(message, 39);
            }
        }


        private void ThreadStart40()
        {
            while (true)
            {
                var message = _messages40.Take();
                ProcessCashOut(message, 40);
            }
        }

        private void ThreadStart41()
        {
            while (true)
            {
                var message = _messages41.Take();
                ProcessCashOut(message, 41);
            }
        }

        private void ThreadStart42()
        {
            while (true)
            {
                var message = _messages42.Take();
                ProcessCashOut(message, 42);
            }
        }

        private void ThreadStart43()
        {
            while (true)
            {
                var message = _messages43.Take();
                ProcessCashOut(message, 43);
            }
        }

        private void ThreadStart44()
        {
            while (true)
            {
                var message = _messages44.Take();
                ProcessCashOut(message, 44);
            }
        }

        private void ThreadStart45()
        {
            while (true)
            {
                var message = _messages45.Take();
                ProcessCashOut(message, 45);
            }
        }

        private void ThreadStart46()
        {
            while (true)
            {
                var message = _messages46.Take();
                ProcessCashOut(message, 46);
            }
        }

        private void ThreadStart47()
        {
            while (true)
            {
                var message = _messages47.Take();
                ProcessCashOut(message, 47);
            }
        }

        private void ThreadStart48()
        {
            while (true)
            {
                var message = _messages48.Take();
                ProcessCashOut(message, 48);
            }
        }

        private void ThreadStart49()
        {
            while (true)
            {
                var message = _messages49.Take();
                ProcessCashOut(message, 49);
            }
        }


        private void ThreadStart50()
        {
            while (true)
            {
                var message = _messages50.Take();
                ProcessCashOut(message, 50);
            }
        }

        private void ThreadStart51()
        {
            while (true)
            {
                var message = _messages51.Take();
                ProcessCashOut(message, 51);
            }
        }

        private void ThreadStart52()
        {
            while (true)
            {
                var message = _messages52.Take();
                ProcessCashOut(message, 52);
            }
        }

        private void ThreadStart53()
        {
            while (true)
            {
                var message = _messages53.Take();
                ProcessCashOut(message, 53);
            }
        }

        private void ThreadStart54()
        {
            while (true)
            {
                var message = _messages54.Take();
                ProcessCashOut(message, 54);
            }
        }

        private void ThreadStart55()
        {
            while (true)
            {
                var message = _messages55.Take();
                ProcessCashOut(message, 55);
            }
        }

        private void ThreadStart56()
        {
            while (true)
            {
                var message = _messages56.Take();
                ProcessCashOut(message, 56);
            }
        }

        private void ThreadStart57()
        {
            while (true)
            {
                var message = _messages57.Take();
                ProcessCashOut(message, 57);
            }
        }

        private void ThreadStart58()
        {
            while (true)
            {
                var message = _messages58.Take();
                ProcessCashOut(message, 58);
            }
        }

        private void ThreadStart59()
        {
            while (true)
            {
                var message = _messages59.Take();
                ProcessCashOut(message, 59);
            }
        }


        private void ThreadStart60()
        {
            while (true)
            {
                var message = _messages60.Take();
                ProcessCashOut(message, 60);
            }
        }

        private void ThreadStart61()
        {
            while (true)
            {
                var message = _messages61.Take();
                ProcessCashOut(message, 61);
            }
        }

        private void ThreadStart62()
        {
            while (true)
            {
                var message = _messages62.Take();
                ProcessCashOut(message, 62);
            }
        }

        private void ThreadStart63()
        {
            while (true)
            {
                var message = _messages63.Take();
                ProcessCashOut(message, 63);
            }
        }

        private void ThreadStart64()
        {
            while (true)
            {
                var message = _messages64.Take();
                ProcessCashOut(message, 64);
            }
        }

        private void ThreadStart65()
        {
            while (true)
            {
                var message = _messages65.Take();
                ProcessCashOut(message, 65);
            }
        }

        private void ThreadStart66()
        {
            while (true)
            {
                var message = _messages66.Take();
                ProcessCashOut(message, 66);
            }
        }

        private void ThreadStart67()
        {
            while (true)
            {
                var message = _messages67.Take();
                ProcessCashOut(message, 67);
            }
        }

        private void ThreadStart68()
        {
            while (true)
            {
                var message = _messages68.Take();
                ProcessCashOut(message, 68);
            }
        }

        private void ThreadStart69()
        {
            while (true)
            {
                var message = _messages69.Take();
                ProcessCashOut(message, 69);
            }
        }


        private void ThreadStart70()
        {
            while (true)
            {
                var message = _messages70.Take();
                ProcessCashOut(message, 70);
            }
        }

        private void ThreadStart71()
        {
            while (true)
            {
                var message = _messages71.Take();
                ProcessCashOut(message, 71);
            }
        }

        private void ThreadStart72()
        {
            while (true)
            {
                var message = _messages72.Take();
                ProcessCashOut(message, 72);
            }
        }

        private void ThreadStart73()
        {
            while (true)
            {
                var message = _messages73.Take();
                ProcessCashOut(message, 73);
            }
        }

        private void ThreadStart74()
        {
            while (true)
            {
                var message = _messages74.Take();
                ProcessCashOut(message, 74);
            }
        }

        private void ThreadStart75()
        {
            while (true)
            {
                var message = _messages75.Take();
                ProcessCashOut(message, 75);
            }
        }

        private void ThreadStart76()
        {
            while (true)
            {
                var message = _messages76.Take();
                ProcessCashOut(message, 76);
            }
        }

        private void ThreadStart77()
        {
            while (true)
            {
                var message = _messages77.Take();
                ProcessCashOut(message, 77);
            }
        }

        private void ThreadStart78()
        {
            while (true)
            {
                var message = _messages78.Take();
                ProcessCashOut(message, 78);
            }
        }

        private void ThreadStart79()
        {
            while (true)
            {
                var message = _messages79.Take();
                ProcessCashOut(message, 79);
            }
        }


        private void ThreadStart80()
        {
            while (true)
            {
                var message = _messages80.Take();
                ProcessCashOut(message, 80);
            }
        }

        private void ThreadStart81()
        {
            while (true)
            {
                var message = _messages81.Take();
                ProcessCashOut(message, 81);
            }
        }

        private void ThreadStart82()
        {
            while (true)
            {
                var message = _messages82.Take();
                ProcessCashOut(message, 82);
            }
        }

        private void ThreadStart83()
        {
            while (true)
            {
                var message = _messages83.Take();
                ProcessCashOut(message, 83);
            }
        }

        private void ThreadStart84()
        {
            while (true)
            {
                var message = _messages84.Take();
                ProcessCashOut(message, 84);
            }
        }

        private void ThreadStart85()
        {
            while (true)
            {
                var message = _messages85.Take();
                ProcessCashOut(message, 85);
            }
        }

        private void ThreadStart86()
        {
            while (true)
            {
                var message = _messages86.Take();
                ProcessCashOut(message, 86);
            }
        }

        private void ThreadStart87()
        {
            while (true)
            {
                var message = _messages87.Take();
                ProcessCashOut(message, 87);
            }
        }

        private void ThreadStart88()
        {
            while (true)
            {
                var message = _messages88.Take();
                ProcessCashOut(message, 88);
            }
        }

        private void ThreadStart89()
        {
            while (true)
            {
                var message = _messages89.Take();
                ProcessCashOut(message, 89);
            }
        }


        private void ThreadStart90()
        {
            while (true)
            {
                var message = _messages90.Take();
                ProcessCashOut(message, 90);
            }
        }

        private void ThreadStart91()
        {
            while (true)
            {
                var message = _messages91.Take();
                ProcessCashOut(message, 91);
            }
        }

        private void ThreadStart92()
        {
            while (true)
            {
                var message = _messages92.Take();
                ProcessCashOut(message, 92);
            }
        }

        private void ThreadStart93()
        {
            while (true)
            {
                var message = _messages93.Take();
                ProcessCashOut(message, 93);
            }
        }

        private void ThreadStart94()
        {
            while (true)
            {
                var message = _messages94.Take();
                ProcessCashOut(message, 94);
            }
        }

        private void ThreadStart95()
        {
            while (true)
            {
                var message = _messages95.Take();
                ProcessCashOut(message, 95);
            }
        }

        private void ThreadStart96()
        {
            while (true)
            {
                var message = _messages96.Take();
                ProcessCashOut(message, 96);
            }
        }

        private void ThreadStart97()
        {
            while (true)
            {
                var message = _messages97.Take();
                ProcessCashOut(message, 97);
            }
        }

        private void ThreadStart98()
        {
            while (true)
            {
                var message = _messages98.Take();
                ProcessCashOut(message, 98);
            }
        }

        private void ThreadStart99()
        {
            while (true)
            {
                var message = _messages99.Take();
                ProcessCashOut(message, 99);
            }
        }

        private int GetIndex()
        {
            if (_index.Count == 0)
            {
                for (int i = 0; i < 100; i++)
                    _index.Push(i);
            }

            return _index.Pop();
        }
    }
}
