using System;

namespace Platform.Shared.Data.Reports.Entities
{
    public class BetTransactionFinal
    {
        public long Id { get; set; }

        public long ParentUserId { get; set; }

        public Guid BetId { get; set; }

        public long UserId { get; set; }

        public string Username { get; set; }

        public int UserTypeId { get; set; }

        public decimal PositionTaking { get; set; }

        public decimal Commission { get; set; }

        public decimal Stake { get; set; }

        public decimal TrueStake { get; set; }

        public decimal Odds { get; set; }

        public int OddsTypeId { get; set; }

        public string OddsTypeName { get; set; }

        public decimal Payout { get; set; }

        public decimal WinLoss { get; set; }

        public int StatusId { get; set; }

        public string StatusName { get; set; }

        public string Reason { get; set; }

        public decimal ExchangeRate { get; set; }

        public string CurrencyCode { get; set; }

        public string IPAddress { get; set; }

        public int Count { get; set; }

        public DateTime TransactionDate { get; set; }

        public DateTime TransactionDateOffset { get; set; }
    }
}
