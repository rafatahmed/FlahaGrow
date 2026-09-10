namespace FlahaGrow.Core.Annual;

/// <summary>Supports only the sky bases used by the annual Radiance workflow.</summary>
public static class AnnualSkySubdivision
{
    public const int Tregenza = 1;
    public const int Reinhart = 4;

    public static int Validate(int subdivision) => subdivision is Tregenza or Reinhart
        ? subdivision
        : throw new ArgumentOutOfRangeException(nameof(subdivision), "Sky subdivision must be 1 (Tregenza) or 4 (Reinhart).");

    public static string ReceiverDirective(int subdivision) => "h=r" + Validate(subdivision);
}
