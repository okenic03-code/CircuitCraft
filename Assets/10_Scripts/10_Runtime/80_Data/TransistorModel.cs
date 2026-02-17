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
        Custom = 7,

        /// <summary>BC548 - General purpose NPN transistor.</summary>
        BC548 = 8,

        /// <summary>BC558 - General purpose PNP transistor.</summary>
        BC558 = 9,

        /// <summary>BC556 - High voltage PNP transistor.</summary>
        BC556 = 10,

        /// <summary>BC337 - Medium power NPN transistor.</summary>
        BC337 = 11,

        /// <summary>TIP31 - NPN power transistor.</summary>
        TIP31 = 12,

        /// <summary>TIP32 - PNP power transistor.</summary>
        TIP32 = 13,

        /// <summary>2N696 - General purpose NPN transistor.</summary>
        _2N696 = 14
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
        Custom = 6,

        /// <summary>BS250 - P-Channel small signal MOSFET.</summary>
        BS250 = 7,

        /// <summary>IRF3205 - N-Channel high power MOSFET.</summary>
        IRF3205 = 8,

        /// <summary>IRF530 - N-Channel power MOSFET.</summary>
        IRF530 = 9,

        /// <summary>IRLZ44N - N-Channel logic level MOSFET.</summary>
        IRLZ44N = 10,

        /// <summary>IRF520 - N-Channel power MOSFET.</summary>
        IRF520 = 11,

        /// <summary>BSS84 - P-Channel small signal MOSFET.</summary>
        BSS84 = 12,

        /// <summary>TP0610L - P-Channel small signal MOSFET.</summary>
        TP0610L = 13,

        /// <summary>FQP27P06 - P-Channel power MOSFET.</summary>
        FQP27P06 = 14
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
        Custom = 13,

        /// <summary>1N4007 - High voltage general purpose rectifier.</summary>
        _1N4007 = 14,

        /// <summary>1N914 - Fast switching signal diode.</summary>
        _1N914 = 15,

        /// <summary>1N4002 - General purpose rectifier (100V).</summary>
        _1N4002 = 16,

        /// <summary>1N4004 - General purpose rectifier (400V).</summary>
        _1N4004 = 17,

        /// <summary>1N5408 - High current rectifier (3A, 1000V).</summary>
        _1N5408 = 18,

        /// <summary>BAS40 - Schottky small signal diode.</summary>
        BAS40 = 19,

        /// <summary>BAT85 - Schottky small signal diode.</summary>
        BAT85 = 20,

        /// <summary>Generic white LED.</summary>
        LED_White = 21,

        /// <summary>Generic yellow LED.</summary>
        LED_Yellow = 22,

        /// <summary>1N4729A - 3.6V zener diode.</summary>
        Zener_3V6 = 23,

        /// <summary>1N4730A - 3.9V zener diode.</summary>
        Zener_3V9 = 24,

        /// <summary>1N4731A - 4.3V zener diode.</summary>
        Zener_4V3 = 25,

        /// <summary>1N4732A - 4.7V zener diode.</summary>
        Zener_4V7 = 26,

        /// <summary>1N4734A - 5.6V zener diode.</summary>
        Zener_5V6 = 27,

        /// <summary>1N4738A - 8.2V zener diode.</summary>
        Zener_8V2 = 28,

        /// <summary>1N4750A - 27V zener diode.</summary>
        Zener_27V = 29
    }
}
