# Manipulandum_data_aquired.cs (not included)

This file is **intentionally absent** from the public repository.

## Why

`Manipulandum_data_aquired.cs` is the Unity-side wrapper that bridges C# to the
native `DAQMANIPULANDUM.dll` hardware driver for the **Dextrain Manipulandum**
isometric force sensor. The class uses `P/Invoke` (`DllImport`) to expose the
native calls `DaqStart`, `DaqStop`, `DaqAcq`, `DaqRead`, `DaqClear`, applies
per-finger calibration, and publishes the five force channels as static arrays
consumed by the rest of the game logic.

Authorship and intellectual-property status of this specific file are
**unclear** to me: the script and the accompanying `DAQMANIPULANDUM.dll` were
provided in the context of a research collaboration with the IFT (ESILV) and
the Dextrain team, and I did not author it. Out of caution, neither the C#
wrapper nor the DLL is redistributed here.

## Interface contract (for readers)

The rest of the code (see [`Phase0_manager.cs`](Phase0_manager.cs) and
[`Data_Logger.cs`](Data_Logger.cs)) consumes this component through a very
small public surface:

- `Manipulandum_data_aquired.Force_Data : double[5]` — calibrated force on
  each finger (thumb → pinky), expressed in newtons, refreshed every frame.
- `Manipulandum_data_aquired.Raw_Data : double[5]` — raw ADC values, same
  index order.
- Sampling is driven from `Update()` at Unity's frame rate; effective
  acquisition frequency is approximately **14 Hz** (limited by the native
  driver).
- A zeroing routine offsets the sensor against its resting baseline at
  session start.

Anyone re-implementing the pipeline with a different force sensor only needs
to provide an equivalent component that publishes `Force_Data[0..4]` in
newtons; no other script in this repository touches the hardware directly.

## Getting the original file

The Dextrain Manipulandum is a research device and the driver is not public.
Access to the sensor and its software stack is managed by:

- **IFT — Institut For The Future**, research lab of ESILV (Paris–La Défense).
- The Dextrain research group (see Térémetz et al., 2023, cited in the
  accompanying paper).
