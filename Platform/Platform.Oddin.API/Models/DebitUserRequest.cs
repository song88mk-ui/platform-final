using System;
using System.Collections.Generic;

namespace Platform.Oddin.API.Models
{
    public class DebitUserRequest
    {
        public DebitUserRequest()
        {
            Amount = new TicketAmount();
            Ticket = new Ticket();
        }
        public string Token { get; set; }
        public string UserId { get; set; }
        public string Currency { get; set; }
        public TicketAmount Amount { get; set; }
        public string Description { get; set; }
        public string TransactionId { get; set; }
        public string TicketId { get; set; }
        public int TransactionType { get; set; }
        public string CreditOperatorTransactionId { get; set; } = "";
        public string CreditTransactionId { get; set; } = null;
        public Ticket Ticket { get; set; } = new Ticket();
    }
    public class Ticket
    {
        public Ticket()
        {
            Stake = new TicketAmount();
            WonAmount = new TicketAmount();
            PossibleWinAmount = new TicketAmount();
            Bets = new List<TicketBet>();
            Date = DateTime.UtcNow;
            Status = "";
            Bets = new List<TicketBet>();
        }
        public TicketAmount Stake { get; set; }
        public TicketAmount WonAmount { get; set; }
        public TicketAmount PossibleWinAmount { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public List<TicketBet> Bets { get; set; }
    }
    public class TicketAmount
    {
        public decimal Amount { get; set; }
        public decimal Tax { get; set; } = 0;
        public decimal TotalAmount { get; set; }
    }
    public class TicketBet
    {
        public TicketBet()
        {
            Stake = new TicketAmount();
            Selections = new List<TicketSelection>();
            WonAmount = new TicketAmount();
            Systems = new List<int>();
        }
        public string Id { get; set; }
        public TicketAmount Stake { get; set; }
        public List<TicketSelection> Selections { get; set; }
        public bool Won { get; set; }
        public bool Voided { get; set; }
        public TicketAmount WonAmount { get; set; }
        public decimal TotalOdds { get; set; }
        public List<int> Systems { get; set; }
    }

    public class TicketSelection
    {
        public TicketSelection()
        {
            Outcome = new TicketOutcome();
            Event = new TicketEvent();
        }
        public string Id { get; set; }
        public TicketOutcome Outcome { get; set; }
        public decimal Odds { get; set; }
        public TicketEvent Event { get; set; }
        public bool Voided { get; set; }
        public bool Won { get; set; }
    }

    public class TicketOutcome
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public string MarketId { get; set; }
        public string MarketName { get; set; }
    }
    public class TicketEvent
    {
        public TicketEvent()
        {
            Teams = new List<TicketEventTeam>();
        }
        public string Id { get; set; }
        public string TournamentId { get; set; }
        public string TournamentName { get; set; }
        public string SportId { get; set; }
        public string SportName { get; set; }
        public DateTime DateStart { get; set; }
        public List<TicketEventTeam> Teams { get; set; }
        public bool IsLive { get; set; }
    }

    public class TicketEventTeam
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public bool IsHome { get; set; }
    }
}
