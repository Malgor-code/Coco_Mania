public readonly struct DebtPaid : IEvent
{
    public readonly int Cycle;
    public readonly float Delivered;
    public readonly float Refund;
    public DebtPaid(int cycle, float delivered, float refund) { Cycle = cycle; Delivered = delivered; Refund = refund; }
}