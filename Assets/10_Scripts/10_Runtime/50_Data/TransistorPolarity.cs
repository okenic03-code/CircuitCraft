namespace CircuitCraft.Data
{
    /// <summary>
    /// Polarity type for bipolar transistors (BJT).
    /// </summary>
    public enum BJTPolarity
    {
        /// <summary>NPN transistor - electrons are majority carriers.</summary>
        NPN,
        
        /// <summary>PNP transistor - holes are majority carriers.</summary>
        PNP
    }
    
    /// <summary>
    /// Channel type for field-effect transistors (FET/MOSFET).
    /// </summary>
    public enum FETPolarity
    {
        /// <summary>N-Channel FET - electrons are majority carriers.</summary>
        NChannel,
        
        /// <summary>P-Channel FET - holes are majority carriers.</summary>
        PChannel
    }
}
