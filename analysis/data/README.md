# Study data (anonymized)

Fifteen CSV files, one per participant (`P01.csv` … `P15.csv`), numbered in
chronological order of acquisition. Original first-name filenames used during
collection have been stripped; the mapping is **not** tracked in this
repository.

## Format

One row per sample, approximately 14 Hz.

| Column         | Unit | Description                                                                 |
|----------------|------|-----------------------------------------------------------------------------|
| `Time(s)`      | s    | Time elapsed since session start.                                           |
| `Phase`        | int  | Experimental phase index (0 = baseline, 1, 2, 3).                           |
| `Progress`     | [-1,1] | Internal progression signal toward the next phase (algorithmic).          |
| `WinnerIndex`  | int  | Index of the finger driving interaction (winner-takes-all rule); `-1` = none. |
| `Force_F0..F4` | N    | Force per finger, thumb → pinky.                                            |
| `Fatigue_F0..F4` | [0,1] | Virtual-fatigue state per finger (active phases 1–2); `NaN` outside.     |
| `P3_Accuracy`  | [0,1] | Sub-maximal convergence score in Phase 3; `NaN` outside.                   |

## Cohort

- N = 15 healthy subjects, 21–63 y, one left-handed.
- Five earlier pilot runs (used for calibration / protocol tuning) are **not**
  included here; they are outside the statistical analysis of the paper.

## Reproducing the analysis

```bash
cd analysis
python -m pip install -r requirements.txt   # pandas, numpy, scipy, matplotlib, seaborn
python analyse.py
python CalculStat.py
```

`analyse.py` and `CalculStat.py` consume the CSVs in the current working
directory. The files are designed to be run from this `data/` folder, or
after copying them next to the scripts.
