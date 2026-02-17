namespace CircuitCraft.Data
{
    /// <summary>
    /// Common BJT transistor models.
    /// </summary>
    public enum BJTModel
    {
        /// <summary>Generic NPN transistor model.</summary>
        Generic_NPN,
        
        /// <summary>Generic PNP transistor model.</summary>
        Generic_PNP,
        
        /// <summary>2N2222 - Popular general-purpose NPN transistor.</summary>
        _2N2222,
        
        /// <summary>2N3904 - Small signal NPN transistor.</summary>
        _2N3904,
        
        /// <summary>2N3906 - Small signal PNP transistor.</summary>
        _2N3906,
        
        /// <summary>BC547 - General purpose NPN transistor.</summary>
        BC547,
        
        /// <summary>BC557 - General purpose PNP transistor.</summary>
        BC557,
        
        /// <summary>Custom model - use custom parameters.</summary>
        Custom
    }
    
    /// <summary>
    /// Common MOSFET transistor models.
    /// </summary>
    public enum MOSFETModel
    {
        /// <summary>Generic N-Channel MOSFET model.</summary>
        Generic_NMOS,
        
        /// <summary>Generic P-Channel MOSFET model.</summary>
        Generic_PMOS,
        
        /// <summary>2N7000 - N-Channel enhancement mode MOSFET.</summary>
        _2N7000,
        
        /// <summary>BS170 - N-Channel small signal MOSFET.</summary>
        BS170,
        
        /// <summary>IRF540 - N-Channel power MOSFET.</summary>
        IRF540,
        
        /// <summary>IRF9540 - P-Channel power MOSFET.</summary>
        IRF9540,
        
        /// <summary>Custom model - use custom parameters.</summary>
        Custom
    }
    
    /// <summary>
    /// Common diode models.
    /// </summary>
    public enum DiodeModel
    {
        /// <summary>Generic silicon diode.</summary>
        Generic,
        
        /// <summary>1N4148 - Fast switching diode.</summary>
        _1N4148,
        
        /// <summary>1N4001 - General purpose rectifier diode.</summary>
        _1N4001,
        
        /// <summary>1N5819 - Schottky barrier diode.</summary>
        _1N5819,
        
        /// <summary>Generic red LED.</summary>
        LED_Red,
        
        /// <summary>Generic green LED.</summary>
        LED_Green,
        
        /// <summary>Generic blue LED.</summary>
        LED_Blue,
        
        /// <summary>1N4728A - 3.3V zener diode.</summary>
        Zener_3V3,
        
        /// <summary>1N4733A - 5.1V zener diode.</summary>
        Zener_5V1,
        
        /// <summary>1N4736A - 6.8V zener diode.</summary>
        Zener_6V8,
        
        /// <summary>1N4739A - 9.1V zener diode.</summary>
        Zener_9V1,
        
        /// <summary>1N4742A - 12V zener diode.</summary>
        Zener_12V,
        
        /// <summary>1N4744A - 15V zener diode.</summary>
        Zener_15V,
        
        /// <summary>Custom model - use custom parameters.</summary>
        Custom
    }
}
