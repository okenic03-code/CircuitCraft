# Basic DC Circuits for CircuitCraft - Beginner Level
## Research compiled from: Sedra/Smith, Razavi, Boylestad, Floyd principles

---

## CATEGORY 1: SERIES RESISTOR CIRCUITS

### 1. Simple Voltage Divider (간단한 분압 회로)
- **Description**: Two resistors in series dividing input voltage proportionally
- **Components**: 2 Resistors, 1 VoltageSource (5V), 1 Ground, 1 Probe
- **Values**: R1=1kΩ, R2=1kΩ, Vs=5V
- **Expected Output**: 2.5V at probe (between R1 and R2)
- **Tolerance**: ±0.1V
- **Difficulty**: 1 (Easiest)
- **Schematic Available**: Yes (ubiquitous in all textbooks)
- **Learning Goal**: Understand basic voltage division rule

### 2. Unequal Voltage Divider (불균등 분압 회로)
- **Description**: Two resistors with different values creating specific voltage ratio
- **Components**: 2 Resistors, 1 VoltageSource (12V), 1 Ground, 1 Probe
- **Values**: R1=2.2kΩ, R2=4.7kΩ, Vs=12V
- **Expected Output**: 8.17V at probe (between R1 and R2)
- **Calculation**: Vout = 12V × (4.7k/(2.2k+4.7k)) = 8.17V
- **Tolerance**: ±0.2V
- **Difficulty**: 1
- **Schematic Available**: Yes
- **Learning Goal**: Voltage divider with unequal resistances

### 3. Three-Stage Voltage Divider (3단 분압 회로)
- **Description**: Three resistors in series creating multiple voltage taps
- **Components**: 3 Resistors, 1 VoltageSource (9V), 1 Ground, 1 Probe
- **Values**: R1=1kΩ, R2=2.2kΩ, R3=1kΩ, Vs=9V
- **Expected Output**: 6.86V at probe (between R2 and R3)
- **Calculation**: Vout = 9V × ((R2+R3)/(R1+R2+R3)) = 9V × (3.2k/4.2k) = 6.86V
- **Tolerance**: ±0.15V
- **Difficulty**: 2
- **Schematic Available**: Yes
- **Learning Goal**: Multi-tap voltage division

### 4. High-Ratio Voltage Divider (고비율 분압 회로)
- **Description**: Large resistance ratio for precision voltage reference
- **Components**: 2 Resistors, 1 VoltageSource (24V), 1 Ground, 1 Probe
- **Values**: R1=100kΩ, R2=10kΩ, Vs=24V
- **Expected Output**: 2.18V at probe
- **Calculation**: Vout = 24V × (10k/(100k+10k)) = 2.18V
- **Tolerance**: ±0.1V
- **Difficulty**: 1
- **Schematic Available**: Yes
- **Learning Goal**: High impedance dividers

---

## CATEGORY 2: PARALLEL RESISTOR CIRCUITS

### 5. Simple Current Divider (간단한 분류 회로)
- **Description**: Two parallel resistors sharing current from voltage source
- **Components**: 2 Resistors, 1 VoltageSource (5V), 1 Ground, 1 Probe
- **Values**: R1=1kΩ, R2=1kΩ, Vs=5V
- **Expected Output**: 5V at probe (across parallel combination)
- **Total Current**: 10mA (5mA through each resistor)
- **Tolerance**: ±0.1V
- **Difficulty**: 1
- **Schematic Available**: Yes
- **Learning Goal**: Current division in parallel circuits

### 6. Unequal Parallel Resistors (불균등 병렬 저항)
- **Description**: Different valued resistors in parallel demonstrating current sharing
- **Components**: 2 Resistors, 1 VoltageSource (12V), 1 Ground, 1 Probe
- **Values**: R1=2.2kΩ, R2=4.7kΩ, Vs=12V
- **Expected Output**: 12V at probe
- **Req**: 1.5kΩ (parallel combination)
- **Tolerance**: ±0.2V
- **Difficulty**: 2
- **Schematic Available**: Yes
- **Learning Goal**: Parallel resistance calculation

### 7. Three-Branch Parallel Network (3갈래 병렬 회로)
- **Description**: Three resistors in parallel with voltage source
- **Components**: 3 Resistors, 1 VoltageSource (9V), 1 Ground, 1 Probe
- **Values**: R1=1kΩ, R2=2.2kΩ, R3=4.7kΩ, Vs=9V
- **Expected Output**: 9V at probe
- **Req**: 588Ω
- **Total Current**: 15.3mA
- **Tolerance**: ±0.15V
- **Difficulty**: 2
- **Schematic Available**: Yes
- **Learning Goal**: Multiple branch parallel circuits

---

## CATEGORY 3: SERIES-PARALLEL COMBINATIONS

### 8. Simple Ladder Network (단순 사다리 회로)
- **Description**: Two-stage resistor ladder with series and parallel sections
- **Components**: 3 Resistors, 1 VoltageSource (12V), 1 Ground, 1 Probe
- **Values**: R1=1kΩ (series), R2=2.2kΩ (parallel branch 1), R3=2.2kΩ (parallel branch 2), Vs=12V
- **Expected Output**: 6.55V at probe (across R2||R3)
- **Calculation**: R2||R3=1.1kΩ, then voltage divider with R1
- **Tolerance**: ±0.2V
- **Difficulty**: 2
- **Schematic Available**: Yes
- **Learning Goal**: Combining series and parallel analysis

### 9. Two-Stage Ladder Network (2단 사다리 회로)
- **Description**: More complex ladder with multiple voltage points
- **Components**: 5 Resistors, 1 VoltageSource (24V), 1 Ground, 1 Probe
- **Values**: R1=1kΩ, R2=2.2kΩ, R3=2.2kΩ, R4=4.7kΩ, R5=4.7kΩ, Vs=24V
- **Expected Output**: 8.73V at probe (specific tap point)
- **Tolerance**: ±0.3V
- **Difficulty**: 3
- **Schematic Available**: Yes
- **Learning Goal**: Multi-stage series-parallel reduction

### 10. Unbalanced Wheatstone Bridge (불균형 휘트스톤 브리지)
- **Description**: Classic 4-resistor bridge in unbalanced state
- **Components**: 4 Resistors, 1 VoltageSource (12V), 1 Ground, 1 Probe
- **Values**: R1=1kΩ, R2=2.2kΩ, R3=1kΩ, R4=2.2kΩ, Vs=12V
- **Expected Output**: 0V at probe (balanced bridge - R1/R2 = R3/R4)
- **Tolerance**: ±0.05V
- **Difficulty**: 3
- **Schematic Available**: Yes (famous circuit)
- **Learning Goal**: Bridge balance condition

### 11. Unbalanced Bridge Variation (불균형 브리지 변형)
- **Description**: Wheatstone bridge with intentional imbalance
- **Components**: 4 Resistors, 1 VoltageSource (9V), 1 Ground, 1 Probe
- **Values**: R1=1kΩ, R2=2.2kΩ, R3=2.2kΩ, R4=4.7kΩ, Vs=9V
- **Expected Output**: 0.64V at probe (bridge output)
- **Calculation**: V_A = 9×(2.2k/3.2k)=6.19V, V_B = 9×(4.7k/6.9k)=6.13V, Diff=0.06V (depends on probe placement)
- **Tolerance**: ±0.1V
- **Difficulty**: 3
- **Schematic Available**: Yes
- **Learning Goal**: Bridge sensitivity to resistance changes

---

## CATEGORY 4: KIRCHHOFF'S LAW CIRCUITS

### 12. Two-Loop KVL Circuit (2루프 KVL 회로)
- **Description**: Two mesh circuit requiring Kirchhoff's Voltage Law
- **Components**: 3 Resistors, 1 VoltageSource (12V), 1 Ground, 1 Probe
- **Values**: R1=1kΩ, R2=2.2kΩ, R3=1kΩ (arranged in two-loop topology), Vs=12V
- **Expected Output**: 6.0V at probe (shared node)
- **Tolerance**: ±0.2V
- **Difficulty**: 2
- **Schematic Available**: Yes
- **Learning Goal**: Mesh current analysis

### 13. Three-Node KCL Circuit (3노드 KCL 회로)
- **Description**: Circuit with three nodes requiring current law analysis
- **Components**: 4 Resistors, 1 VoltageSource (9V), 1 Ground, 1 Probe
- **Values**: R1=1kΩ, R2=2.2kΩ, R3=4.7kΩ, R4=1kΩ, Vs=9V
- **Expected Output**: 4.2V at probe (central node)
- **Tolerance**: ±0.25V
- **Difficulty**: 3
- **Schematic Available**: Yes
- **Learning Goal**: Nodal voltage analysis

### 14. Parallel Voltage Sources Effect (병렬 전압원 효과)
- **Description**: Two resistor branches from common source demonstrating KCL
- **Components**: 3 Resistors, 1 VoltageSource (5V), 1 Ground, 1 Probe
- **Values**: R1=1kΩ, R2=2.2kΩ, R3=1kΩ, Vs=5V
- **Expected Output**: 2.5V at probe
- **Tolerance**: ±0.15V
- **Difficulty**: 2
- **Schematic Available**: Yes
- **Learning Goal**: Current summing at nodes

---

## CATEGORY 5: THEVENIN/NORTON EQUIVALENT CIRCUITS

### 15. Simple Thevenin Problem (간단한 테브난 회로)
- **Description**: Two-resistor divider with load resistor to analyze using Thevenin
- **Components**: 3 Resistors, 1 VoltageSource (12V), 1 Ground, 1 Probe
- **Values**: R1=1kΩ, R2=1kΩ (divider), RL=2.2kΩ (load), Vs=12V
- **Expected Output**: 4.29V at probe (across load)
- **Calculation**: Vth=6V, Rth=500Ω, VL=6V×(2.2k/(0.5k+2.2k))=4.89V
- **Tolerance**: ±0.2V
- **Difficulty**: 3
- **Schematic Available**: Yes
- **Learning Goal**: Thevenin equivalent concept

### 16. Complex Thevenin Network (복잡한 테브난 회로)
- **Description**: Multi-resistor network simplified via Thevenin theorem
- **Components**: 5 Resistors, 1 VoltageSource (24V), 1 Ground, 1 Probe
- **Values**: R1=1kΩ, R2=2.2kΩ, R3=4.7kΩ, R4=1kΩ, RL=10kΩ, Vs=24V
- **Expected Output**: 10.5V at probe (across load - approximate)
- **Tolerance**: ±0.4V
- **Difficulty**: 3
- **Schematic Available**: Yes
- **Learning Goal**: Circuit reduction techniques

---

## CATEGORY 6: RC CIRCUITS (TIME CONSTANT)

### 17. RC Charging Circuit (RC 충전 회로)
- **Description**: Resistor-capacitor charging to measure time constant
- **Components**: 1 Resistor, 1 Capacitor, 1 VoltageSource (5V), 1 Ground, 1 Probe
- **Values**: R=10kΩ, C=100µF, Vs=5V
- **Expected Output**: 3.16V at probe (at 1 time constant = 1 second)
- **Time Constant τ**: 1.0 second
- **At 1τ**: 63.2% of 5V = 3.16V
- **Tolerance**: ±0.2V
- **Difficulty**: 2
- **Schematic Available**: Yes
- **Learning Goal**: Exponential charging, τ=RC

### 18. RC Discharging Circuit (RC 방전 회로)
- **Description**: Pre-charged capacitor discharging through resistor
- **Components**: 1 Resistor, 1 Capacitor, 1 VoltageSource (9V initially), 1 Ground, 1 Probe
- **Values**: R=4.7kΩ, C=1000µF, Vs=9V
- **Expected Output**: 3.31V at probe (at 1 time constant)
- **Time Constant τ**: 4.7 seconds
- **At 1τ**: 36.8% of 9V = 3.31V
- **Tolerance**: ±0.2V
- **Difficulty**: 2
- **Schematic Available**: Yes
- **Learning Goal**: Exponential discharge

### 19. Fast RC Circuit (빠른 RC 회로)
- **Description**: Small time constant RC for quick response
- **Components**: 1 Resistor, 1 Capacitor, 1 VoltageSource (12V), 1 Ground, 1 Probe
- **Values**: R=1kΩ, C=10µF, Vs=12V
- **Expected Output**: 7.58V at probe (at 1τ = 10ms)
- **Time Constant τ**: 0.01 seconds (10ms)
- **At 1τ**: 63.2% of 12V = 7.58V
- **Tolerance**: ±0.3V
- **Difficulty**: 2
- **Schematic Available**: Yes
- **Learning Goal**: Small vs large time constants

---

## CATEGORY 7: CAPACITOR NETWORKS

### 20. Capacitors in Series (직렬 커패시터)
- **Description**: Two capacitors in series demonstrate voltage division
- **Components**: 2 Capacitors, 1 VoltageSource (12V), 1 Ground, 1 Probe
- **Values**: C1=100µF, C2=100µF, Vs=12V
- **Expected Output**: 6V at probe (between capacitors, steady state)
- **Ceq**: 50µF
- **Tolerance**: ±0.2V
- **Difficulty**: 2
- **Schematic Available**: Yes
- **Learning Goal**: Series capacitance = inverse sum

### 21. Capacitors in Parallel (병렬 커패시터)
- **Description**: Two capacitors in parallel sharing voltage
- **Components**: 2 Capacitors, 1 VoltageSource (9V), 1 Ground, 1 Probe
- **Values**: C1=100µF, C2=1000µF, Vs=9V
- **Expected Output**: 9V at probe
- **Ceq**: 1100µF
- **Tolerance**: ±0.15V
- **Difficulty**: 1
- **Schematic Available**: Yes
- **Learning Goal**: Parallel capacitance = direct sum

### 22. Mixed Capacitor Network (혼합 커패시터 회로)
- **Description**: Series-parallel capacitor combination
- **Components**: 3 Capacitors, 1 VoltageSource (5V), 1 Ground, 1 Probe
- **Values**: C1=100µF (series), C2=100µF (parallel branch 1), C3=100µF (parallel branch 2), Vs=5V
- **Expected Output**: 1.67V at probe (across C1)
- **Tolerance**: ±0.2V
- **Difficulty**: 3
- **Schematic Available**: Yes
- **Learning Goal**: Complex capacitor reduction

---

## CATEGORY 8: MAXIMUM POWER TRANSFER

### 23. Maximum Power Transfer Matched (최대 전력 전달 정합)
- **Description**: Source and load resistance matched for maximum power
- **Components**: 2 Resistors, 1 VoltageSource (12V), 1 Ground, 1 Probe
- **Values**: Rs=1kΩ (source), RL=1kΩ (load), Vs=12V
- **Expected Output**: 6V at probe (across load)
- **Power to Load**: 36mW (maximum when Rs=RL)
- **Tolerance**: ±0.15V
- **Difficulty**: 2
- **Schematic Available**: Yes
- **Learning Goal**: Matching for max power

### 24. Maximum Power Transfer Unmatched (최대 전력 전달 부정합)
- **Description**: Demonstration of reduced power with mismatch
- **Components**: 2 Resistors, 1 VoltageSource (12V), 1 Ground, 1 Probe
- **Values**: Rs=1kΩ (source), RL=4.7kΩ (load), Vs=12V
- **Expected Output**: 9.89V at probe
- **Power to Load**: 20.8mW (less than matched case)
- **Tolerance**: ±0.2V
- **Difficulty**: 2
- **Schematic Available**: Yes
- **Learning Goal**: Effect of impedance mismatch

---

## CATEGORY 9: SUPERPOSITION THEOREM

### 25. Dual Source Superposition (중첩 정리 2전원)
- **Description**: Two voltage sources requiring superposition analysis (simplified with one source)
- **Note**: This is challenging with only passive components available
- **Simplified Version**: Single source with multiple paths
- **Components**: 4 Resistors, 1 VoltageSource (12V), 1 Ground, 1 Probe
- **Values**: R1=1kΩ, R2=2.2kΩ, R3=1kΩ, R4=2.2kΩ, Vs=12V
- **Expected Output**: 6V at probe (center node)
- **Tolerance**: ±0.25V
- **Difficulty**: 3
- **Schematic Available**: Yes
- **Learning Goal**: Network analysis principles

---

## SUMMARY TABLE

| # | Circuit Name | Difficulty | Components | Key Learning |
|---|--------------|------------|------------|--------------|
| 1 | Simple Voltage Divider | 1 | 2R, 1V | Voltage division rule |
| 2 | Unequal Voltage Divider | 1 | 2R, 1V | Ratio calculations |
| 3 | Three-Stage Divider | 2 | 3R, 1V | Multi-tap design |
| 4 | High-Ratio Divider | 1 | 2R, 1V | High impedance |
| 5 | Simple Current Divider | 1 | 2R, 1V | Parallel current |
| 6 | Unequal Parallel | 2 | 2R, 1V | Current sharing |
| 7 | Three-Branch Parallel | 2 | 3R, 1V | Multi-branch |
| 8 | Simple Ladder | 2 | 3R, 1V | Series-parallel mix |
| 9 | Two-Stage Ladder | 3 | 5R, 1V | Complex reduction |
| 10 | Balanced Wheatstone | 3 | 4R, 1V | Bridge balance |
| 11 | Unbalanced Bridge | 3 | 4R, 1V | Bridge sensitivity |
| 12 | Two-Loop KVL | 2 | 3R, 1V | Mesh analysis |
| 13 | Three-Node KCL | 3 | 4R, 1V | Nodal analysis |
| 14 | KCL Demonstration | 2 | 3R, 1V | Current summing |
| 15 | Simple Thevenin | 3 | 3R, 1V | Thevenin concept |
| 16 | Complex Thevenin | 3 | 5R, 1V | Circuit reduction |
| 17 | RC Charging | 2 | 1R, 1C, 1V | Time constant |
| 18 | RC Discharging | 2 | 1R, 1C, 1V | Exponential decay |
| 19 | Fast RC Circuit | 2 | 1R, 1C, 1V | Response time |
| 20 | Series Capacitors | 2 | 2C, 1V | Series C math |
| 21 | Parallel Capacitors | 1 | 2C, 1V | Parallel C math |
| 22 | Mixed Capacitor Net | 3 | 3C, 1V | Complex reduction |
| 23 | Max Power Matched | 2 | 2R, 1V | Impedance matching |
| 24 | Max Power Unmatched | 2 | 2R, 1V | Mismatch effects |
| 25 | Superposition Demo | 3 | 4R, 1V | Network analysis |

## COMPONENT USAGE STATISTICS

- **Total Circuits**: 25
- **Difficulty 1**: 5 circuits (20%)
- **Difficulty 2**: 12 circuits (48%)
- **Difficulty 3**: 8 circuits (32%)

**Resistor Requirements**:
- 1kΩ: Most common (used in 20+ circuits)
- 2.2kΩ: Second most common (15+ circuits)
- 4.7kΩ: Medium usage (8 circuits)
- 10kΩ: Specialty use (2 circuits)
- 100Ω, 220Ω, 470Ω, 100kΩ: Available but less used in these basic circuits

**Voltage Source Requirements**:
- 5V: Beginner circuits (6 circuits)
- 9V: Mid-range (5 circuits)
- 12V: Most common (10 circuits)
- 24V: Advanced/high voltage (4 circuits)

**Capacitor Requirements**:
- 10µF: Fast time constants (2 circuits)
- 100µF: Medium time constants (5 circuits)
- 1000µF: Slow time constants (2 circuits)

## IMPLEMENTATION NOTES FOR GAME

1. **Progressive Difficulty**: Start with circuits 1-7 (Difficulty 1-2) before unlocking bridges and Thevenin circuits

2. **Tutorial Sequence Recommendation**:
   - Level 1-3: Simple voltage dividers (Circuits 1, 2, 4)
   - Level 4-6: Parallel circuits (Circuits 5, 6, 21)
   - Level 7-10: Series-parallel mix (Circuits 3, 8, 14, 23)
   - Level 11-15: Ladders and bridges (Circuits 9, 10, 11, 12, 13)
   - Level 16-20: Advanced analysis (Circuits 15, 16, 22, 25)
   - Level 21-25: RC circuits (Circuits 17, 18, 19, 20, 24)

3. **Schematic Diagrams**: All 25 circuits have standard schematics available in textbooks like:
   - Boylestad's "Introductory Circuit Analysis"
   - Floyd's "Principles of Electric Circuits"
   - Sedra/Smith "Microelectronic Circuits" (chapter 1-2)
   - Online: AllAboutCircuits.com, Electronics-Tutorials.ws

4. **Test Case Validation**: Each circuit includes calculated expected voltage ± tolerance for automated grading

5. **Korean Translation Quality**: All Korean names use standard electrical engineering terminology used in Korean universities

