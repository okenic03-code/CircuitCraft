# CircuitCraft: Comprehensive Circuit Catalog

This catalog organizes 100 circuit puzzles for the CircuitCraft game, divided into 10 progressive difficulty worlds.

## WORLD 1: Basic Passive (Difficulty 1)

## 1. Simple Voltage Divider / 단순 분압기
- **Difficulty**: 1 ★☆☆☆☆
- **Category**: Passive
- **Components**: R1=1k, R2=1k, Vs=5V, Ground
- **Circuit Description**: A basic two-resistor network to halve the source voltage.
- **Test Case**: Probe at node between R1 and R2 → Expected 2.5V ±0.1V
- **Learning Objective**: Understand series resistance and proportional voltage drop.
- **Reference**: Ohm's Law / Voltage Division
- **Diagram**: NEEDED

## 2. Unequal Voltage Divider / 비대칭 분압기
- **Difficulty**: 1 ★☆☆☆☆
- **Category**: Passive
- **Components**: R1=1k, R2=2.2k, Vs=9V, Ground
- **Circuit Description**: A voltage divider using different resistor values to achieve a specific ratio.
- **Test Case**: Probe at node between R1 and R2 → Expected 6.19V ±0.2V
- **Learning Objective**: Calculate voltage division with non-identical resistors.
- **Reference**: Voltage Divider Formula
- **Diagram**: NEEDED

## 3. Three-Resistor Series / 3저항 직렬 회로
- **Difficulty**: 1 ★☆☆☆☆
- **Category**: Passive
- **Components**: R1=470, R2=1k, R3=2.2k, Vs=12V, Ground
- **Circuit Description**: Three resistors connected in series to divide 12V into multiple steps.
- **Test Case**: Probe across R2 → Expected 3.27V ±0.2V
- **Learning Objective**: Extend series voltage division concepts to multiple components.
- **Reference**: KVL (Kirchhoff's Voltage Law)
- **Diagram**: NEEDED

## 4. Parallel Resistors / 병렬 저항 회로
- **Difficulty**: 1 ★☆☆☆☆
- **Category**: Passive
- **Components**: R1=1k, R2=2.2k (parallel), Vs=5V, Ground
- **Circuit Description**: Two resistors connected in parallel across a single source.
- **Test Case**: Probe total current → Expected 7.27mA ±0.1V (node voltage 5V)
- **Learning Objective**: Learn that parallel components share the same voltage.
- **Reference**: Parallel Resistance
- **Diagram**: NEEDED

## 5. Current Divider / 전류 분배기
- **Difficulty**: 1 ★☆☆☆☆
- **Category**: Passive
- **Components**: R1=1k, R2=4.7k (parallel), Vs=9V, Ground
- **Circuit Description**: Parallel network designed to split current between two paths.
- **Test Case**: Current through R1 → Expected 9mA ±0.1V
- **Learning Objective**: Understand how current splits inversely to resistance.
- **Reference**: KCL (Kirchhoff's Current Law)
- **Diagram**: NEEDED

## 6. LED with Current Limiter / LED 전류 제한 회로
- **Difficulty**: 1 ★☆☆☆☆
- **Category**: Passive
- **Components**: R=470, LED_Red, Vs=5V, Ground
- **Circuit Description**: A simple resistor-LED series circuit to prevent burnout.
- **Test Case**: Voltage across R → Expected 3V ±0.3V
- **Learning Objective**: Use resistors to protect sensitive components.
- **Reference**: LED Forward Voltage
- **Diagram**: NEEDED

## 7. Series LED Circuit / 직렬 LED 회로
- **Difficulty**: 1 ★☆☆☆☆
- **Category**: Passive
- **Components**: R=220, LED_Red × 2, Vs=9V, Ground
- **Circuit Description**: Driving multiple LEDs in series with a single current-limiting resistor.
- **Test Case**: Voltage across R → Expected 5V ±0.5V
- **Learning Objective**: Calculate cumulative forward voltage drops in a series string.
- **Reference**: Series Components
- **Diagram**: NEEDED

## 8. Two-Source Series / 이중 전원 직렬 회로
- **Difficulty**: 1 ★☆☆☆☆
- **Category**: Passive
- **Components**: Vs1=5V, Vs2=9V, R=1k, Ground
- **Circuit Description**: Two voltage sources connected in series to increase total potential.
- **Test Case**: Voltage at R high side → Expected 14V ±0.1V
- **Learning Objective**: Learn how voltage sources stack.
- **Reference**: Superposition (Basic)
- **Diagram**: NEEDED

## 9. Basic Ground Reference / 기본 접지 기준 회로
- **Difficulty**: 1 ★☆☆☆☆
- **Category**: Passive
- **Components**: Vs=12V, R1=1k, R2=1k, Ground
- **Circuit Description**: Establishing a virtual mid-point using a balanced divider.
- **Test Case**: Probe mid-node → Expected 6V ±0.1V
- **Learning Objective**: Understand the importance of ground as a reference point.
- **Reference**: Reference Nodes
- **Diagram**: NEEDED

## 10. Triple Voltage Divider / 3단 분압기
- **Difficulty**: 1 ★☆☆☆☆
- **Category**: Passive
- **Components**: R1=R2=R3=1k, Vs=9V, Ground
- **Circuit Description**: A string of three identical resistors creating two reference taps.
- **Test Case**: Probe node 1 → Expected 6V; node 2 → Expected 3V ±0.2V
- **Learning Objective**: Multi-tap voltage division.
- **Reference**: Series Resistance
- **Diagram**: NEEDED

---

## WORLD 2: Intermediate Passive (Difficulty 2)

## 11. Loaded Voltage Divider / 부하형 분압기
- **Difficulty**: 2 ★★☆☆☆
- **Category**: Passive
- **Components**: R1=1k, R2=2.2k, R_load=4.7k, Vs=12V, Ground
- **Circuit Description**: A voltage divider where the output voltage is affected by an attached load resistor.
- **Test Case**: Probe Vout → Expected 7.15V ±0.3V
- **Learning Objective**: Understand loading effects and parallel resistance in dividers.
- **Reference**: Loading Effect
- **Diagram**: NEEDED

## 12. R-2R Ladder Network / R-2R 래더 네트워크
- **Difficulty**: 2 ★★☆☆☆
- **Category**: Passive
- **Components**: R=1k, 2R=2.2k (approx), Vs=5V, Ground
- **Circuit Description**: A basic structure used in Digital-to-Analog conversion.
- **Test Case**: Probe node output → Expected specific binary weighted voltage ±0.2V
- **Learning Objective**: Learn iterative resistance structures.
- **Reference**: DAC Architectures
- **Diagram**: NEEDED

## 13. Wheatstone Bridge Balanced / 평형 휘트스톤 브릿지
- **Difficulty**: 2 ★★☆☆☆
- **Category**: Passive
- **Components**: R1=R2=R3=R4=1k, Vs=9V, Ground
- **Circuit Description**: A bridge configuration where the differential voltage between legs is zero.
- **Test Case**: Differential voltage (V_bridge) → Expected 0V ±0.1V
- **Learning Objective**: Understand balanced bridge conditions.
- **Reference**: Bridge Measurements
- **Diagram**: NEEDED

## 14. Wheatstone Bridge Unbalanced / 비평형 휘트스톤 브릿지
- **Difficulty**: 2 ★★☆☆☆
- **Category**: Passive
- **Components**: R1=1k, R2=2.2k, R3=1k, R4=4.7k, Vs=12V, Ground
- **Circuit Description**: A bridge with mismatched resistors creating a measurable differential voltage.
- **Test Case**: Differential voltage (V_bridge) → Expected non-zero value ±0.2V
- **Learning Objective**: Calculate output for unbalanced sensor bridges.
- **Reference**: Sensor Circuitry
- **Diagram**: NEEDED

## 15. T-Network Attenuator / T형 감쇠기
- **Difficulty**: 2 ★★☆☆☆
- **Category**: Passive
- **Components**: R1=470, R2=1k, R3=470, Vs=12V, Ground
- **Circuit Description**: A specific three-resistor network for signal level reduction.
- **Test Case**: Probe Vout → Expected attenuated voltage ±0.3V
- **Learning Objective**: Understand impedance matching and attenuation.
- **Reference**: Network Synthesis
- **Diagram**: NEEDED

## 16. Pi-Network Attenuator / π형 감쇠기
- **Difficulty**: 2 ★★☆☆☆
- **Category**: Passive
- **Components**: R1=2.2k, R2=1k, R3=2.2k, Vs=9V, Ground
- **Circuit Description**: A parallel-series-parallel arrangement for signal attenuation.
- **Test Case**: Probe Vout → Expected attenuated voltage ±0.3V
- **Learning Objective**: Compare Pi and T topologies.
- **Reference**: RF Attenuation
- **Diagram**: NEEDED

## 17. Superposition Two Sources / 중첩 원리 회로
- **Difficulty**: 2 ★★☆☆☆
- **Category**: Passive
- **Components**: Vs1=5V, Vs2=9V, R1=1k, R2=2.2k, R3=4.7k, Ground
- **Circuit Description**: A multi-loop network where nodal analysis or superposition must be used.
- **Test Case**: Probe Vout at central node → Expected specific sum of source effects ±0.3V
- **Learning Objective**: Apply the principle of superposition in linear circuits.
- **Reference**: Superposition Theorem
- **Diagram**: NEEDED

## 18. Thevenin Equivalent / 테브난 등가 회로
- **Difficulty**: 2 ★★☆☆☆
- **Category**: Passive
- **Components**: Vs=12V, R1=1k, R2=2.2k, R_load=4.7k, Ground
- **Circuit Description**: Simplifying a complex network into a single voltage source and series resistor.
- **Test Case**: Voltage across R_load → Expected value matching Thevenin calculation ±0.2V
- **Learning Objective**: Learn to simplify circuits for easier analysis.
- **Reference**: Thevenin's Theorem
- **Diagram**: NEEDED

## 19. Maximum Power Transfer / 최대 전력 전달
- **Difficulty**: 2 ★★☆☆☆
- **Category**: Passive
- **Components**: Vs=9V, R_source=1k, R_load=1k, Ground
- **Circuit Description**: Demonstrating that power is maximized when load resistance equals source resistance.
- **Test Case**: Probe V_load → Expected 4.5V (half of Vs) ±0.1V
- **Learning Objective**: Understand the relationship between source and load impedance.
- **Reference**: Maximum Power Transfer Theorem
- **Diagram**: NEEDED

## 20. Kirchhoff Two-Loop / 키르히호프 2루프 회로
- **Difficulty**: 2 ★★☆☆☆
- **Category**: Passive
- **Components**: Vs=12V, R1=1k, R2=2.2k, R3=4.7k, Ground
- **Circuit Description**: A classic two-mesh network requiring simultaneous equations.
- **Test Case**: Probe specific node voltage → Expected value ±0.3V
- **Learning Objective**: Practice multi-loop nodal and mesh analysis.
- **Reference**: Kirchhoff's Laws
- **Diagram**: NEEDED

---

## WORLD 3: Diode Basics (Difficulty 2)

## 21. Single Diode Forward / 단순 다이오드 순방향
- **Difficulty**: 2 ★★☆☆☆
- **Category**: Diode
- **Components**: D=1N4148, R=1k, Vs=5V, Ground
- **Circuit Description**: Basic diode conduction with a current limiting resistor.
- **Test Case**: Voltage across R → Expected 4.3V ±0.2V
- **Learning Objective**: Learn about diode forward voltage drop (Vf).
- **Reference**: PN Junction Characteristics
- **Diagram**: NEEDED

## 22. Half-Wave Rectifier / 반파 정류기
- **Difficulty**: 2 ★★☆☆☆
- **Category**: Diode
- **Components**: D=1N4007, R=1k, Vs=12V, Ground
- **Circuit Description**: Removing the negative portion of an input signal (modeled with positive DC for conduction).
- **Test Case**: Vout after diode → Expected 11.3V (Vs - Vf) ±0.5V
- **Learning Objective**: Understand how diodes act as one-way valves.
- **Reference**: Rectification
- **Diagram**: NEEDED

## 23. Diode OR Gate / 다이오드 OR 게이트
- **Difficulty**: 2 ★★☆☆☆
- **Category**: Diode
- **Components**: D1=D2=1N4148, R=10k, Vs1=Vs2=5V, Ground
- **Circuit Description**: Implementing logical OR using two diodes and a pull-down resistor.
- **Test Case**: Vout with either source active → Expected 4.3V ±0.2V
- **Learning Objective**: Basics of diode logic (DTL precursor).
- **Reference**: Digital Logic Fundamentals
- **Diagram**: NEEDED

## 24. Diode AND Gate / 다이오드 AND 게이트
- **Difficulty**: 2 ★★☆☆☆
- **Category**: Diode
- **Components**: D1=D2=1N4148, R=10k, Vs=5V, Ground
- **Circuit Description**: Implementing logical AND using diodes and a pull-up resistor.
- **Test Case**: Vout when inputs are high → Expected logic high value ±0.2V
- **Learning Objective**: Use diodes for intersection logic.
- **Reference**: Logic Gates
- **Diagram**: NEEDED

## 25. Series Positive Clipper / 직렬 양전압 클리퍼
- **Difficulty**: 2 ★★☆☆☆
- **Category**: Diode
- **Components**: D=1N4148, R=1k, Vs=12V, Ground
- **Circuit Description**: A circuit that blocks voltages above a certain threshold.
- **Test Case**: Vout at cathode → Expected clipped value ≈ 0.7V ±0.2V
- **Learning Objective**: Learn how diodes can limit signal excursions.
- **Reference**: Wave Shaping
- **Diagram**: NEEDED

## 26. Parallel Negative Clipper / 병렬 음전압 클리퍼
- **Difficulty**: 2 ★★☆☆☆
- **Category**: Diode
- **Components**: D=1N4148, R=1k, Vs=12V, Ground
- **Circuit Description**: Using a diode in parallel with the output to clip negative voltages.
- **Test Case**: Output voltage → Expected clipped negative swing ±0.2V
- **Learning Objective**: Understand parallel vs series clipping.
- **Reference**: Diode Applications
- **Diagram**: NEEDED

## 27. LED Current Limiter / LED 전류 제한기
- **Difficulty**: 2 ★★☆☆☆
- **Category**: Diode
- **Components**: R=470, LED_Red, Vs=9V, Ground
- **Circuit Description**: Basic protection circuit for a single Red LED.
- **Test Case**: Voltage across R → Expected ≈ 7V ±0.3V
- **Learning Objective**: Calculate resistance for target LED current.
- **Reference**: Optoelectronics
- **Diagram**: NEEDED

## 28. Series LED Array / 직렬 LED 어레이
- **Difficulty**: 2 ★★☆☆☆
- **Category**: Diode
- **Components**: R=220, LED_Red × 3, Vs=12V, Ground
- **Circuit Description**: Multiple LEDs in series, sharing the same current.
- **Test Case**: Voltage across R → Expected ≈ 6V ±0.5V
- **Learning Objective**: Manage multiple Vf drops in a single branch.
- **Reference**: Series Strings
- **Diagram**: NEEDED

## 29. Dual Clipper / 이중 제한 회로
- **Difficulty**: 2 ★★☆☆☆
- **Category**: Diode
- **Components**: D1=D2=1N4148 (anti-parallel), R=1k, Vs=12V, Ground
- **Circuit Description**: Two diodes in opposite directions to clip both positive and negative peaks.
- **Test Case**: Output limited to ±0.7V → Expected ±0.7V ±0.1V
- **Learning Objective**: Learn signal bounding techniques.
- **Reference**: Overvoltage Protection
- **Diagram**: NEEDED

## 30. Diode Voltage Drop / 다이오드 전압강하 회로
- **Difficulty**: 2 ★★☆☆☆
- **Category**: Diode
- **Components**: D1=D2=1N4148 (series), R=1k, Vs=5V, Ground
- **Circuit Description**: Using the cumulative Vf of series diodes to drop a supply voltage.
- **Test Case**: Vout after 2 diodes → Expected 3.6V ±0.2V
- **Learning Objective**: Use diodes as simple voltage offsets.
- **Reference**: Biasing Networks
- **Diagram**: NEEDED

---

## WORLD 4: Zener & Rectifier (Difficulty 3)

## 31. Basic Zener Regulator 5V / 5V 제너 레귤레이터
- **Difficulty**: 3 ★★★☆☆
- **Category**: Zener
- **Components**: Z=5.1V, R=470, Vs=12V, Ground
- **Circuit Description**: A simple shunt regulator using a Zener diode to maintain 5.1V.
- **Test Case**: Vout across Zener → Expected 5.1V ±0.1V
- **Learning Objective**: Learn the principle of Zener breakdown for regulation.
- **Reference**: Shunt Regulators
- **Diagram**: NEEDED

## 32. Zener Regulator 9V / 9V 제너 레귤레이터
- **Difficulty**: 3 ★★★☆☆
- **Category**: Zener
- **Components**: Z=9.1V, R=1k, Vs=24V, Ground
- **Circuit Description**: Higher voltage regulation using a 9.1V Zener.
- **Test Case**: Vout across Zener → Expected 9.1V ±0.2V
- **Learning Objective**: Scale Zener circuits for different supply levels.
- **Reference**: Voltage Stabilization
- **Diagram**: NEEDED

## 33. Zener with Load / 부하형 제너 회로
- **Difficulty**: 3 ★★★☆☆
- **Category**: Zener
- **Components**: Z=5.1V, R_series=470, R_load=1k, Vs=12V, Ground
- **Circuit Description**: Evaluating how a load resistor impacts Zener regulation quality.
- **Test Case**: Vout across load → Expected 5.1V ±0.1V
- **Learning Objective**: Understand Zener current requirements under load.
- **Reference**: Load Regulation
- **Diagram**: NEEDED

## 34. Full Bridge Rectifier / 전파 브릿지 정류기
- **Difficulty**: 3 ★★★☆☆
- **Category**: Diode
- **Components**: D × 4=1N4007, R=1k, Vs=12V, Ground
- **Circuit Description**: Using four diodes to convert AC to full-wave DC (simulated with polarity).
- **Test Case**: Vout across R → Expected rectified voltage ±0.5V
- **Learning Objective**: Learn bridge topology and double Vf drops.
- **Reference**: Power Supply Design
- **Diagram**: NEEDED

## 35. Voltage Doubler / 배전압 회로
- **Difficulty**: 3 ★★★☆☆
- **Category**: Mixed
- **Components**: D × 2=1N4148, C × 2=10µF, R=10k, Vs=12V, Ground
- **Circuit Description**: A capacitor-diode network that creates an output twice the input peak.
- **Test Case**: Vout across final capacitor → Expected ≈ 24V ±1V
- **Learning Objective**: Combine reactive and non-linear components for voltage boosting.
- **Reference**: Villard Cascade
- **Diagram**: NEEDED

## 36. LED Voltage Indicator / LED 전압 표시기
- **Difficulty**: 3 ★★★☆☆
- **Category**: Zener
- **Components**: Z=8.2V, LED_Yellow, R=470, Vs=12V, Ground
- **Circuit Description**: An LED that only turns on when the supply exceeds a specific Zener threshold.
- **Test Case**: Turn-on threshold → Expected ≈ 10.2V ±0.3V
- **Learning Objective**: Use Zeners as simple voltage comparators/thresholds.
- **Reference**: Status Indicators
- **Diagram**: NEEDED

## 37. Peak Detector / 피크 검출기
- **Difficulty**: 3 ★★★☆☆
- **Category**: Diode
- **Components**: D=1N4148, C=100µF, R=100k, Vs=12V, Ground
- **Circuit Description**: A circuit that captures and holds the highest voltage seen at the input.
- **Test Case**: V_cap after pulse → Expected Vpeak - Vf ±0.5V
- **Learning Objective**: Learn about charge storage and diode blocking.
- **Reference**: Signal Processing
- **Diagram**: NEEDED

## 38. Dual Zener Reference / 이중 기준 전압
- **Difficulty**: 3 ★★★☆☆
- **Category**: Zener
- **Components**: Z1=12V, Z2=5.1V, R1=1k, R2=470, Vs=24V, Ground
- **Circuit Description**: Creating two stable reference voltages from a single high-voltage rail.
- **Test Case**: Probe V1 and V2 → Expected 12V and 5.1V ±0.2V
- **Learning Objective**: Manage multiple regulation points.
- **Reference**: Analog Reference Design
- **Diagram**: NEEDED

## 39. Zener Overvoltage Protection / 과전압 보호 회로
- **Difficulty**: 3 ★★★☆☆
- **Category**: Zener
- **Components**: Z=15V, R=1k, R_load=2.2k, Vs=24V, Ground
- **Circuit Description**: Protecting a load by shunting excess voltage through a Zener.
- **Test Case**: V_load with 24V input → Expected ≈ 15V ±0.3V
- **Learning Objective**: Use Zeners for fail-safe clamping.
- **Reference**: Circuit Protection
- **Diagram**: NEEDED

## 40. Biased Clipper / 바이어스 클리퍼
- **Difficulty**: 3 ★★★☆☆
- **Category**: Diode
- **Components**: D=1N4148, Vs_bias=5V, R=1k, Vs=12V, Ground
- **Circuit Description**: A clipper circuit where the clipping level is set by an external bias voltage.
- **Test Case**: Output clipping level → Expected 5.7V ±0.2V
- **Learning Objective**: Shift diode clipping levels using secondary sources.
- **Reference**: Signal Level Limiting
- **Diagram**: NEEDED

---

## WORLD 5: BJT Switching (Difficulty 3)

## 41. NPN Switch / NPN 스위치
- **Difficulty**: 3 ★★★☆☆
- **Category**: BJT
- **Components**: Q=2N2222, R_base=10k, R_c=1k, Vs=5V, Ground
- **Circuit Description**: Using a small base current to switch a larger collector current.
- **Test Case**: Vce when base is high → Expected ≈ 0.2V ±0.1V
- **Learning Objective**: Understand saturation and cutoff modes in BJTs.
- **Reference**: Transistor Switching
- **Diagram**: NEEDED

## 42. PNP Switch / PNP 스위치
- **Difficulty**: 3 ★★★☆☆
- **Category**: BJT
- **Components**: Q=2N3906, R_base=10k, R_c=1k, Vs=5V, Ground
- **Circuit Description**: Complementary switching where pulling the base low turns the device on.
- **Test Case**: Vce when base is low → Expected ≈ 0.2V ±0.1V
- **Learning Objective**: Master the operation of PNP transistors.
- **Reference**: High-Side Switching
- **Diagram**: NEEDED

## 43. NPN LED Driver / NPN LED 드라이버
- **Difficulty**: 3 ★★★☆☆
- **Category**: BJT
- **Components**: Q=2N2222, R_base=10k, R_c=220, LED_Red, Vs=5V, Ground
- **Circuit Description**: A common application using a BJT to drive a high-current LED.
- **Test Case**: Voltage across LED → Expected ≈ 2V ±0.3V
- **Learning Objective**: Combine switching and diode concepts.
- **Reference**: Driver Circuits
- **Diagram**: NEEDED

## 44. Fixed Bias / 고정 바이어스
- **Difficulty**: 3 ★★★☆☆
- **Category**: BJT
- **Components**: Q=BC547, R_base=100k, R_c=1k, Vs=12V, Ground
- **Circuit Description**: A simple biasing scheme using a single resistor from the supply to the base.
- **Test Case**: Collector voltage (Vc) → Expected ≈ 6V ±1V
- **Learning Objective**: Learn the basics of setting a DC operating point (Q-point).
- **Reference**: Amplifier Biasing
- **Diagram**: NEEDED

## 45. Collector Feedback Bias / 컬렉터 피드백 바이어스
- **Difficulty**: 3 ★★★☆☆
- **Category**: BJT
- **Components**: Q=BC547, R_f=100k, R_c=1k, Vs=12V, Ground
- **Circuit Description**: Improving bias stability by connecting the base resistor to the collector.
- **Test Case**: Collector voltage (Vc) → Expected stable midpoint ±1V
- **Learning Objective**: Introduction to negative feedback for stability.
- **Reference**: Feedback Biasing
- **Diagram**: NEEDED

## 46. Voltage Divider Bias / 분압 바이어스
- **Difficulty**: 3 ★★★☆☆
- **Category**: BJT
- **Components**: Q=2N2222, R1=10k, R2=2.2k, R_c=1k, R_e=470, Vs=12V, Ground
- **Circuit Description**: The most common and stable biasing method using a resistor divider and emitter resistor.
- **Test Case**: Collector voltage (Vc) → Expected target bias point ±1V
- **Learning Objective**: Master the "Universal" bias circuit.
- **Reference**: Beta-Independent Biasing
- **Diagram**: NEEDED

## 47. Emitter Bias / 이미터 바이어스
- **Difficulty**: 3 ★★★☆☆
- **Category**: BJT
- **Components**: Q=BC548, R_b=100k, R_c=2.2k, R_e=1k, Vs=12V, Ground
- **Circuit Description**: Biasing using an emitter resistor to provide local feedback.
- **Test Case**: Collector voltage (Vc) → Expected value ±1V
- **Learning Objective**: Understand how emitter resistance stabilizes the collector current.
- **Reference**: Bias Stabilization
- **Diagram**: NEEDED

## 48. BJT NOT Gate / BJT NOT 게이트
- **Difficulty**: 3 ★★★☆☆
- **Category**: BJT
- **Components**: Q=2N2222, R_base=10k, R_c=1k, Vs=5V, Ground
- **Circuit Description**: Implementing a logical inverter using a single NPN transistor.
- **Test Case**: Vout with High input → Expected ≈ 0.2V; Low input → Expected 5V ±0.2V
- **Learning Objective**: Bridge the gap between analog components and digital logic.
- **Reference**: RTL (Resistor-Transistor Logic)
- **Diagram**: NEEDED

## 49. Transistor Current Source / 트랜지스터 전류원
- **Difficulty**: 3 ★★★☆☆
- **Category**: BJT
- **Components**: Q=BC547, R_e=1k, R_bias=10k, Z=4.7V, Vs=12V, Ground
- **Circuit Description**: Creating a constant current output that is independent of the load resistance.
- **Test Case**: Current through collector → Expected ≈ 4mA ±0.5V
- **Learning Objective**: Learn how BJTs can act as controlled current regulators.
- **Reference**: Constant Current Sources
- **Diagram**: NEEDED

## 50. BJT Level Shifter / BJT 레벨 시프터
- **Difficulty**: 3 ★★★☆☆
- **Category**: BJT
- **Components**: Q=2N2222, R1=4.7k, R2=10k, Vs=12V, Ground
- **Circuit Description**: Shifting a signal from a low-voltage logic level to a higher voltage.
- **Test Case**: Vout high level → Expected ≈ 12V ±0.5V
- **Learning Objective**: Interface different voltage domains.
- **Reference**: Interfacing Circuits
- **Diagram**: NEEDED

---

## WORLD 6: BJT Amplifiers (Difficulty 4)

## 51. Common Emitter Amplifier / 공통 이미터 증폭기
- **Difficulty**: 4 ★★★★☆
- **Category**: BJT
- **Components**: Q=2N2222, R1=10k, R2=2.2k, Rc=1k, Re=470, Vs=12V, Ground
- **Circuit Description**: Standard amplifier configuration providing high voltage gain and phase inversion.
- **Test Case**: Quiescent Vc → Expected ≈ 7V ±0.5V
- **Learning Objective**: Understand the basic CE configuration and gain factors.
- **Reference**: Small Signal Amplifiers
- **Diagram**: NEEDED

## 52. CE with Bypass Cap / 바이패스 CE 증폭기
- **Difficulty**: 4 ★★★★☆
- **Category**: BJT
- **Components**: Q=BC547, R1=10k, R2=2.2k, Rc=2.2k, Re=1k, C=100µF, Vs=12V, Ground
- **Circuit Description**: Using a bypass capacitor to increase AC gain while maintaining DC stability.
- **Test Case**: Quiescent Vc → Expected mid-rail value ±0.5V
- **Learning Objective**: Distinguish between DC and AC analysis of amplifiers.
- **Reference**: AC Analysis
- **Diagram**: NEEDED

## 53. Emitter Follower / 이미터 팔로워
- **Difficulty**: 4 ★★★★☆
- **Category**: BJT
- **Components**: Q=2N2222, R1=10k, R2=4.7k, Re=1k, Vs=9V, Ground
- **Circuit Description**: Common Collector configuration providing unity voltage gain and high current gain.
- **Test Case**: Emitter voltage (Ve) → Expected Vb - 0.7V ±0.3V
- **Learning Objective**: Learn about impedance matching and voltage tracking.
- **Reference**: Common Collector
- **Diagram**: NEEDED

## 54. Common Base Amplifier / 공통 베이스 증폭기
- **Difficulty**: 4 ★★★★☆
- **Category**: BJT
- **Components**: Q=BC547, Re=1k, Rc=4.7k, Vs=12V, Ground
- **Circuit Description**: Amplifier with low input impedance and high output impedance, used for high-frequency.
- **Test Case**: Collector voltage (Vc) → Expected biased value ±1V
- **Learning Objective**: Explore alternative transistor orientations.
- **Reference**: High-Frequency Models
- **Diagram**: NEEDED

## 55. Darlington Pair / 달링턴 쌍
- **Difficulty**: 4 ★★★★☆
- **Category**: BJT
- **Components**: Q1=Q2=2N2222, R_base=100k, R_load=100, Vs=12V, Ground
- **Circuit Description**: Two transistors connected to achieve extremely high current gain (Beta squared).
- **Test Case**: Ve across load → Expected Vb - 1.4V ±0.5V
- **Learning Objective**: Learn about compound transistor structures.
- **Reference**: Darlington Configuration
- **Diagram**: NEEDED

## 56. Basic Current Mirror / 기본 전류 미러
- **Difficulty**: 4 ★★★★☆
- **Category**: BJT
- **Components**: Q1=Q2=2N3904, R_ref=4.7k, R_load=4.7k, Vs=12V, Ground
- **Circuit Description**: A classic analog circuit that replicates a reference current in another branch.
- **Test Case**: Current through R_load → Expected ≈ I_ref ±0.3V
- **Learning Objective**: Introduction to matched transistor circuits.
- **Reference**: Current Mirrors
- **Diagram**: NEEDED

## 57. PNP Current Source / PNP 전류원
- **Difficulty**: 4 ★★★★☆
- **Category**: BJT
- **Components**: Q=2N3906, R_e=2.2k, R_bias, Vs=12V, Ground
- **Circuit Description**: High-side current source using a PNP transistor.
- **Test Case**: Output current → Expected specific programmed value ±0.5V
- **Learning Objective**: Use PNP devices for sourcing current from the positive rail.
- **Reference**: Active Loads
- **Diagram**: NEEDED

## 58. CE Amplifier with Active Load / 능동 부하 CE 증폭기
- **Difficulty**: 4 ★★★★☆
- **Category**: BJT
- **Components**: Q_npn=2N2222, Q_pnp=2N3906, R_bias, Vs=12V, Ground
- **Circuit Description**: Replacing a collector resistor with a current source to maximize gain.
- **Test Case**: Vc at junction → Expected mid-rail bias ±0.5V
- **Learning Objective**: Learn about high-impedance loads in IC design.
- **Reference**: Integrated Circuit Design
- **Diagram**: NEEDED

## 59. Cascode Amplifier / 캐스코드 증폭기
- **Difficulty**: 4 ★★★★☆
- **Category**: BJT
- **Components**: Q1=Q2=BC547, biasing resistors, Vs=12V, Ground
- **Circuit Description**: A two-transistor stack (CE+CB) to reduce Miller effect and increase bandwidth.
- **Test Case**: Vc of top transistor → Expected value ±1V
- **Learning Objective**: Advanced gain-bandwidth product techniques.
- **Reference**: Cascode Configuration
- **Diagram**: NEEDED

## 60. Two-Stage CE+CC / 2단 CE+CC 증폭기
- **Difficulty**: 4 ★★★★☆
- **Category**: BJT
- **Components**: Q1=2N2222, Q2=2N2222, biasing, Vs=12V, Ground
- **Circuit Description**: A multi-stage amplifier combining voltage gain (CE) with low output impedance (CC).
- **Test Case**: Vout at second stage → Expected DC and AC behavior ±0.5V
- **Learning Objective**: Master stage coupling and cascading.
- **Reference**: Multistage Amplifiers
- **Diagram**: NEEDED

---

## WORLD 7: MOSFET Circuits (Difficulty 3-4)

## 61. NMOS Switch / NMOS 스위치
- **Difficulty**: 3 ★★★☆☆
- **Category**: MOSFET
- **Components**: M=2N7000, R_gate=10k, R_d=1k, Vs=5V, Ground
- **Circuit Description**: Using a gate voltage to control current flow between drain and source.
- **Test Case**: Vds when Vgs is high → Expected ≈ 0.1V ±0.1V
- **Learning Objective**: Understand voltage-controlled switching vs current-controlled.
- **Reference**: FET Switching
- **Diagram**: NEEDED

## 62. PMOS Switch / PMOS 스위치
- **Difficulty**: 3 ★★★☆☆
- **Category**: MOSFET
- **Components**: M=BS250, R_gate=10k, R_d=1k, Vs=5V, Ground
- **Circuit Description**: High-side switch that turns on when the gate is pulled low.
- **Test Case**: Vds when Vgs is low → Expected ≈ 0.1V ±0.1V
- **Learning Objective**: Learn the operation of P-channel enhancement MOSFETs.
- **Reference**: PMOS Logic
- **Diagram**: NEEDED

## 63. MOSFET LED Driver / MOSFET LED 드라이버
- **Difficulty**: 3 ★★★☆☆
- **Category**: MOSFET
- **Components**: M=2N7000, R_gate=10k, R_d=220, LED, Vs=5V, Ground
- **Circuit Description**: Driving an LED with zero steady-state gate current.
- **Test Case**: Voltage across LED → Expected target forward voltage ±0.3V
- **Learning Objective**: Practical FET application for low-power control.
- **Reference**: High-Z Interfacing
- **Diagram**: NEEDED

## 64. CMOS Inverter / CMOS 인버터
- **Difficulty**: 3 ★★★☆☆
- **Category**: MOSFET
- **Components**: M_n=2N7000, M_p=BS250, Vs=5V, Ground
- **Circuit Description**: The fundamental building block of modern digital logic using N and P pairs.
- **Test Case**: Vout with High/Low inputs → Expected rail-to-rail swing ±0.1V
- **Learning Objective**: Learn the concept of complementary logic.
- **Reference**: CMOS Technology
- **Diagram**: NEEDED

## 65. Common Source Amplifier / 공통 소스 증폭기
- **Difficulty**: 4 ★★★★☆
- **Category**: MOSFET
- **Components**: M=2N7000, R_d=4.7k, R_g=100k, R_s=1k, Vs=12V, Ground
- **Circuit Description**: Standard FET amplifier with high input impedance.
- **Test Case**: Drain voltage (Vd) → Expected bias point ±1V
- **Learning Objective**: Biasing MOSFETs for linear amplification.
- **Reference**: FET Amplifiers
- **Diagram**: NEEDED

## 66. Source Follower / 소스 팔로워
- **Difficulty**: 4 ★★★★☆
- **Category**: MOSFET
- **Components**: M=2N7000, R_s=1k, R_g=100k, Vs=9V, Ground
- **Circuit Description**: Common Drain configuration used as a voltage buffer.
- **Test Case**: Source voltage (Vs_node) → Expected Vg - Vth ±0.5V
- **Learning Objective**: Understand impedance transformation in FETs.
- **Reference**: Common Drain
- **Diagram**: NEEDED

## 67. Common Gate Amplifier / 공통 게이트 증폭기
- **Difficulty**: 4 ★★★★☆
- **Category**: MOSFET
- **Components**: M=2N7000, R_d=4.7k, R_s=1k, Vs=12V, Ground
- **Circuit Description**: Low input impedance amplifier used for isolation or high-frequency.
- **Test Case**: Drain voltage (Vd) → Expected biased value ±1V
- **Learning Objective**: Comparative study of CG vs CB amplifiers.
- **Reference**: FET Topologies
- **Diagram**: NEEDED

## 68. MOSFET Current Source / MOSFET 전류원
- **Difficulty**: 4 ★★★★☆
- **Category**: MOSFET
- **Components**: M=2N7000, R_s=1k, Vs=12V, Ground
- **Circuit Description**: Providing a constant current using FET saturation characteristics.
- **Test Case**: Drain current → Expected stable value ±0.5V
- **Learning Objective**: Constant current generation with zero gate load.
- **Reference**: FET Current Sources
- **Diagram**: NEEDED

## 69. NMOS Power Switch / NMOS 파워 스위치
- **Difficulty**: 4 ★★★★☆
- **Category**: MOSFET
- **Components**: M=IRLZ44N, R_gate=100, R_load=100, Vs=12V, Ground
- **Circuit Description**: Handling high currents with a logic-level power MOSFET.
- **Test Case**: Voltage across load → Expected ≈ 12V ±0.3V
- **Learning Objective**: Learn about gate charge and power FET basics.
- **Reference**: Power Electronics
- **Diagram**: NEEDED

## 70. Logic Level Shifter / 로직 레벨 시프터
- **Difficulty**: 4 ★★★★☆
- **Category**: MOSFET
- **Components**: M=2N7000, R1=10k, R2=10k, Vs_low=5V, Vs_high=12V, Ground
- **Circuit Description**: Bidirectional-capable level shifter using a MOSFET and pull-up resistors.
- **Test Case**: High-side Vout → Expected 12V level ±0.3V
- **Learning Objective**: Interface 5V and 12V systems safely.
- **Reference**: Bus Level Shifting
- **Diagram**: NEEDED

---

## WORLD 8: MOSFET Advanced (Difficulty 4)

## 71. CMOS NAND Gate / CMOS NAND 게이트
- **Difficulty**: 4 ★★★★☆
- **Category**: MOSFET
- **Components**: M_n × 2=2N7000, M_p × 2=BS250, Vs=5V, Ground
- **Circuit Description**: Parallel P-channel and series N-channel transistors to implement NAND.
- **Test Case**: Vout with High inputs → Expected Low output ±0.1V
- **Learning Objective**: Construct complex logic from individual transistors.
- **Reference**: Digital IC Design
- **Diagram**: NEEDED

## 72. CMOS NOR Gate / CMOS NOR 게이트
- **Difficulty**: 4 ★★★★☆
- **Category**: MOSFET
- **Components**: M_n × 2=2N7000, M_p × 2=BS250, Vs=5V, Ground
- **Circuit Description**: Series P-channel and parallel N-channel transistors to implement NOR.
- **Test Case**: Vout with either input High → Expected Low output ±0.1V
- **Learning Objective**: Compare NAND and NOR CMOS topologies.
- **Reference**: CMOS Logic Design
- **Diagram**: NEEDED

## 73. CS with Source Degeneration / 소스 디제너레이션 CS
- **Difficulty**: 4 ★★★★☆
- **Category**: MOSFET
- **Components**: M=2N7000, R_d=4.7k, R_s=470, Vs=12V, Ground
- **Circuit Description**: Adding a source resistor to stabilize gain and increase input range.
- **Test Case**: Vd voltage → Expected stabilized bias ±1V
- **Learning Objective**: Use local feedback to manage MOSFET non-linearity.
- **Reference**: Feedback Amplifiers
- **Diagram**: NEEDED

## 74. MOSFET Current Mirror / MOSFET 전류 미러
- **Difficulty**: 4 ★★★★☆
- **Category**: MOSFET
- **Components**: M1=M2=2N7000, R_ref=4.7k, R_load=4.7k, Vs=12V, Ground
- **Circuit Description**: FET version of the current mirror, relying on Vgs matching.
- **Test Case**: Output current → Expected mirror ratio accuracy ±0.5V
- **Learning Objective**: Contrast BJT and FET mirror performance.
- **Reference**: Analog Integrated Circuits
- **Diagram**: NEEDED

## 75. PMOS Active Load / PMOS 능동 부하
- **Difficulty**: 4 ★★★★☆
- **Category**: MOSFET
- **Components**: M_n=2N7000, M_p=BS250, biasing, Vs=12V, Ground
- **Circuit Description**: A CS amplifier with a PMOS current source as the drain load.
- **Test Case**: Vd junction → Expected high-gain bias point ±1V
- **Learning Objective**: Maximize voltage gain in a single stage.
- **Reference**: High Gain Topologies
- **Diagram**: NEEDED

## 76. Cascode MOSFET / 캐스코드 MOSFET
- **Difficulty**: 4 ★★★★☆
- **Category**: MOSFET
- **Components**: M1=M2=2N7000, biasing, Vs=12V, Ground
- **Circuit Description**: Stacking MOSFETs to improve output impedance and frequency response.
- **Test Case**: Drain voltage of top FET → Expected value ±1V
- **Learning Objective**: Learn advanced FET stacking techniques.
- **Reference**: Cascode FETs
- **Diagram**: NEEDED

## 77. MOSFET H-Bridge / MOSFET H-브릿지
- **Difficulty**: 4 ★★★★☆
- **Category**: MOSFET
- **Components**: M_n × 2, M_p × 2, R_load, Vs=12V, Ground
- **Circuit Description**: Four-transistor network used to reverse voltage across a load (motor driver).
- **Test Case**: Voltage polarity across R_load → Expected ±12V swing ±0.5V
- **Learning Objective**: Control high-power bidirectional loads.
- **Reference**: Motor Drivers
- **Diagram**: NEEDED

## 78. MOSFET Voltage Regulator / MOSFET 전압 레귤레이터
- **Difficulty**: 4 ★★★★☆
- **Category**: MOSFET
- **Components**: M=IRF540, Z=5.1V, R, Vs=12V, Ground
- **Circuit Description**: A series pass regulator using a power MOSFET.
- **Test Case**: Vout → Expected 5.1V - Vgs threshold ±0.2V
- **Learning Objective**: Use FETs for high-current regulation.
- **Reference**: Power Supplies
- **Diagram**: NEEDED

## 79. High-Side MOSFET Switch / 하이사이드 MOSFET 스위치
- **Difficulty**: 4 ★★★★☆
- **Category**: MOSFET
- **Components**: M_p=IRF9540, R_gate, R_load, Vs=12V, Ground
- **Circuit Description**: Switching the positive rail to a load using a P-channel power FET.
- **Test Case**: V_load when ON → Expected ≈ 12V ±0.3V
- **Learning Objective**: Practical implementation of high-side power control.
- **Reference**: Load Switching
- **Diagram**: NEEDED

## 80. MOSFET Voltage Follower / MOSFET 전압 팔로워
- **Difficulty**: 4 ★★★★☆
- **Category**: MOSFET
- **Components**: M=IRF540, R_s=1k, Z=5.1V, Vs=12V, Ground
- **Circuit Description**: High-current buffer using a power MOSFET and Zener reference.
- **Test Case**: Vs output → Expected Zener voltage offset by Vgs ±0.3V
- **Learning Objective**: Buffer sensitive reference voltages for heavy loads.
- **Reference**: Buffering Techniques
- **Diagram**: NEEDED

---

## WORLD 9: Multi-Transistor (Difficulty 4-5)

## 81. Wilson Current Mirror / 윌슨 전류 미러
- **Difficulty**: 4 ★★★★☆
- **Category**: BJT
- **Components**: Q × 3=BC547, R_ref=4.7k, Vs=12V, Ground
- **Circuit Description**: A three-transistor mirror that significantly reduces the error due to base currents.
- **Test Case**: Output current match → Expected high precision ±0.3V
- **Learning Objective**: Understand how to mitigate BJT non-idealities.
- **Reference**: Precision Mirrors
- **Diagram**: NEEDED

## 82. Widlar Current Source / 위들러 전류원
- **Difficulty**: 4 ★★★★☆
- **Category**: BJT
- **Components**: Q1=Q2=BC547, R_ref=4.7k, R_e=1k, Vs=12V, Ground
- **Circuit Description**: A current source that can produce very small currents using standard resistor values.
- **Test Case**: Output current → Expected specific micro-amp scale value ±0.3V
- **Learning Objective**: Learn logarithmic current generation.
- **Reference**: Widlar Source
- **Diagram**: NEEDED

## 83. Differential Pair / 차동 증폭기
- **Difficulty**: 4 ★★★★☆
- **Category**: BJT
- **Components**: Q1=Q2=2N3904, R_c × 2=4.7k, R_ee=10k, Vs=12V, Ground
- **Circuit Description**: The fundamental input stage of an Op-Amp, amplifying the difference between two signals.
- **Test Case**: Balanced Vc1 and Vc2 → Expected symmetrical bias ±0.5V
- **Learning Objective**: Learn about differential signaling and common-mode rejection.
- **Reference**: Long-Tailed Pair
- **Diagram**: NEEDED

## 84. Diff Pair with Active Load / 능동 부하 차동 증폭기
- **Difficulty**: 5 ★★★★★
- **Category**: BJT
- **Components**: Q_npn × 2, Q_pnp × 2, Vs=12V, Ground
- **Circuit Description**: Combining a differential pair with a current mirror load for extreme gain.
- **Test Case**: Single-ended Vout → Expected high sensitivity ±1V
- **Learning Objective**: Internal structure of modern operational amplifiers.
- **Reference**: Op-Amp Gain Stages
- **Diagram**: NEEDED

## 85. Push-Pull Class B / 푸시풀 B급 출력단
- **Difficulty**: 4 ★★★★☆
- **Category**: BJT
- **Components**: Q_npn=TIP31, Q_pnp=TIP32, R_load=100, Vs=12V, Ground
- **Circuit Description**: Complementary output stage for driving loads with high efficiency.
- **Test Case**: Vout centered at zero → Expected midpoint bias ±0.5V
- **Learning Objective**: Understand crossover distortion and power efficiency.
- **Reference**: Power Amplifiers
- **Diagram**: NEEDED

## 86. Class AB with Vbe Multiplier / AB급 Vbe 바이어스
- **Difficulty**: 5 ★★★★★
- **Category**: BJT
- **Components**: Q × 3, R_bias, R_load, Vs=12V, Ground
- **Circuit Description**: Eliminating crossover distortion by using a third transistor as a voltage spreader.
- **Test Case**: Quiescent current → Expected low but non-zero value ±0.5V
- **Learning Objective**: Learn advanced thermal and bias compensation.
- **Reference**: Audio Amplifier Design
- **Diagram**: NEEDED

## 87. Schmitt Trigger / 슈미트 트리거
- **Difficulty**: 4 ★★★★☆
- **Category**: BJT
- **Components**: Q × 2=2N2222, R1=10k, R2=1k, R3=4.7k, Vs=5V, Ground
- **Circuit Description**: A regenerative comparator that adds hysteresis to clean up noisy signals.
- **Test Case**: Switching threshold (Vth) → Expected upper and lower trip points ±0.3V
- **Learning Objective**: Introduction to positive feedback and hysteresis.
- **Reference**: Regenerative Circuits
- **Diagram**: NEEDED

## 88. Astable Multivibrator / 비안정 멀티바이브레이터
- **Difficulty**: 4 ★★★★☆
- **Category**: BJT
- **Components**: Q × 2=2N2222, R × 2=10k, C × 2=10µF, Vs=5V, Ground
- **Circuit Description**: A free-running oscillator circuit that generates square waves.
- **Test Case**: Output oscillation → Expected frequency ±0.5V (time domain)
- **Learning Objective**: Learn how RC timing and transistors create oscillation.
- **Reference**: Relaxation Oscillators
- **Diagram**: NEEDED

## 89. Shunt Voltage Regulator / 션트 전압 레귤레이터
- **Difficulty**: 4 ★★★★☆
- **Category**: BJT
- **Components**: Q=2N2222, Z=5.1V, R_series=470, Vs=12V, Ground
- **Circuit Description**: Using a transistor to amplify the Zener's current handling capability in shunt mode.
- **Test Case**: Vout → Expected Zener + Vbe ≈ 5.8V ±0.2V
- **Learning Objective**: Combine Zeners and BJTs for better regulation.
- **Reference**: Power Control
- **Diagram**: NEEDED

## 90. Series Pass Regulator / 시리즈 패스 레귤레이터
- **Difficulty**: 4 ★★★★☆
- **Category**: BJT
- **Components**: Q=TIP31, Z=5.1V, R_base=1k, R_load=100, Vs=12V, Ground
- **Circuit Description**: A basic linear regulator where the transistor is in series with the load.
- **Test Case**: Vout across load → Expected Zener - Vbe ≈ 4.4V ±0.3V
- **Learning Objective**: Foundation of linear power supply design.
- **Reference**: Linear Regulators
- **Diagram**: NEEDED

---

## WORLD 10: Expert Mixed (Difficulty 5)

## 91. Complete Power Supply / 완전한 전원 공급기
- **Difficulty**: 5 ★★★★★
- **Category**: Mixed
- **Components**: D × 4=1N4007, C=1000µF, Q=TIP31, Z=5.1V, Vs=12V, Ground
- **Circuit Description**: A full integration of rectification, smoothing, and regulation.
- **Test Case**: Clean DC Vout → Expected 5V ±0.3V
- **Learning Objective**: System-level integration of circuit blocks.
- **Reference**: AC-DC Conversion
- **Diagram**: NEEDED

## 92. Constant Current LED Driver / 정전류 LED 드라이버
- **Difficulty**: 5 ★★★★★
- **Category**: Mixed
- **Components**: Q=2N2222, R_e=470, LED × 2, Vs=12V, Ground
- **Circuit Description**: A high-precision current regulator for multiple LEDs using feedback.
- **Test Case**: LED Current → Expected constant value across voltage range ±0.5V
- **Learning Objective**: Precision lighting control.
- **Reference**: Constant Current Drivers
- **Diagram**: NEEDED

## 93. Temperature Compensated Bias / 온도 보상 바이어스
- **Difficulty**: 5 ★★★★★
- **Category**: Mixed
- **Components**: Q=2N2222, D=1N4148, resistors, Vs=12V, Ground
- **Circuit Description**: Using diode voltage drops to counteract the Vbe temperature coefficient of a BJT.
- **Test Case**: Vc stability → Expected minimal drift ±0.5V
- **Learning Objective**: Learn about thermal effects and compensation.
- **Reference**: Thermal Stability
- **Diagram**: NEEDED

## 94. Bandgap Reference Concept / 밴드갭 기준 개념
- **Difficulty**: 5 ★★★★★
- **Category**: Mixed
- **Components**: Q × 2, R × 3, Vs=12V, Ground
- **Circuit Description**: A simplified version of a temperature-independent voltage reference.
- **Test Case**: Output Vref → Expected ≈ 1.25V ±0.2V
- **Learning Objective**: Advanced physics-based reference design.
- **Reference**: Bandgap References
- **Diagram**: NEEDED

## 95. Cascode Current Mirror / 캐스코드 전류 미러
- **Difficulty**: 5 ★★★★★
- **Category**: BJT
- **Components**: Q × 4=BC547, R_ref=4.7k, Vs=12V, Ground
- **Circuit Description**: Combining cascode stacking with current mirrors for high output impedance.
- **Test Case**: Mirror current → Expected extremely high compliance ±0.3V
- **Learning Objective**: Professional-grade analog circuit design.
- **Reference**: High Compliance Mirrors
- **Diagram**: NEEDED

## 96. BiCMOS Inverter / BiCMOS 인버터
- **Difficulty**: 5 ★★★★★
- **Category**: Mixed
- **Components**: Q_bjt + M_mos, R, Vs=5V, Ground
- **Circuit Description**: Using MOSFETs for high input impedance and BJTs for high output drive.
- **Test Case**: Vout switching speed/drive → Expected hybrid performance ±0.2V
- **Learning Objective**: Combine the strengths of BJT and FET technologies.
- **Reference**: BiCMOS Logic
- **Diagram**: NEEDED

## 97. Soft-Start Circuit / 소프트 스타트 회로
- **Difficulty**: 5 ★★★★★
- **Category**: Mixed
- **Components**: Q=2N2222, C=1000µF, R, Vs=12V, Ground
- **Circuit Description**: Using a capacitor ramp to slowly turn on a transistor, limiting inrush current.
- **Test Case**: Vout ramp time → Expected gradual increase to 12V ±0.5V
- **Learning Objective**: Manage transient behavior in power systems.
- **Reference**: Transient Response
- **Diagram**: NEEDED

## 98. Overvoltage Protection / 과전압 보호 회로
- **Difficulty**: 5 ★★★★★
- **Category**: Mixed
- **Components**: Q=2N2222, Z=12V, LED_Red, R, Vs=24V, Ground
- **Circuit Description**: A circuit that actively disconnects or flags when input voltage is unsafe.
- **Test Case**: V_protected output → Expected clamp or zero at high Vs ±0.5V
- **Learning Objective**: Fail-safe engineering.
- **Reference**: Crowbar Circuits
- **Diagram**: NEEDED

## 99. Two-Stage Diff Amp / 2단 차동 증폭기
- **Difficulty**: 5 ★★★★★
- **Category**: BJT
- **Components**: Q × 4, resistors, Vs=12V, Ground
- **Circuit Description**: A cascaded differential amplifier for significantly higher gain and CMRR.
- **Test Case**: Final Vout → Expected high differential gain ±1V
- **Learning Objective**: Master the core architecture of high-performance analog ICs.
- **Reference**: Precision Amplifiers
- **Diagram**: NEEDED

## 100. Regulated Power Supply with Foldback / 폴드백 보호 전원
- **Difficulty**: 5 ★★★★★
- **Category**: Mixed
- **Components**: Q × 2=TIP31 + 2N2222, Z, D, R, Vs=24V, Ground
- **Circuit Description**: A high-end regulator that reduces output current during a short circuit to protect itself.
- **Test Case**: Short-circuit current → Expected minimal current flow ±0.5V
- **Learning Objective**: Advanced protection schemes in power electronics.
- **Reference**: Foldback Current Limiting
- **Diagram**: NEEDED
