import os
import glob
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
from scipy import stats

# ---------------------------------------------------------
# CONFIGURATION & AESTHETICS
# ---------------------------------------------------------
sns.set_theme(style="whitegrid", palette="muted")
FORCE_COLS = ['Force_F0', 'Force_F1', 'Force_F2', 'Force_F3', 'Force_F4']

# ---------------------------------------------------------
# 1. DATA EXTRACTION & ANONYMIZATION
# ---------------------------------------------------------
csv_files = glob.glob("*.csv")
data_subjects = {}
anonym_map = {}

print("--- ANONYMIZATION REGISTER (KEEP PRIVATE) ---")
for i, filepath in enumerate(csv_files):
    raw_name = os.path.basename(filepath).split('_')[0]
    p_label = f"P{i+1}"
    anonym_map[p_label] = raw_name
    print(f"{p_label} : {raw_name}")
    
    df = pd.read_csv(filepath)
    df.columns = df.columns.str.strip()
    
    # Calculate Net Isolation Force (F_max - F_second_max)
    def calc_net_force(row):
        forces = sorted([row[c] for c in FORCE_COLS], reverse=True)
        return forces[0] - forces[1]
    df['Net_Force'] = df.apply(calc_net_force, axis=1)
    
    data_subjects[p_label] = df

# ---------------------------------------------------------
# 2. METRICS CALCULATION PER SUBJECT
# ---------------------------------------------------------
results = []
phase3_normalized = pd.DataFrame()

for p_label, df in data_subjects.items():
    df_active = df[df[FORCE_COLS].max(axis=1) > 0.5].copy()
    
    p0 = df_active[df_active['Phase'] == 0]
    p2 = df_active[df_active['Phase'] == 2]
    p3 = df[df['Phase'] == 3].copy()
    
    # A. INDIVIDUATION
    net_0 = p0['Net_Force'].mean() if not p0.empty else np.nan
    net_2 = p2['Net_Force'].mean() if not p2.empty else np.nan
    
    p1_active = df_active[df_active['Phase'] == 1]
    net_1_early = p1_active.iloc[:len(p1_active)//3]['Net_Force'].mean() if len(p1_active) > 20 else np.nan
    net_1_late = p1_active.iloc[-len(p1_active)//3:]['Net_Force'].mean() if len(p1_active) > 20 else np.nan

    # B. CONVERGENCE (Accuracy in Phase 3)
    acc_early = np.nan
    acc_late = np.nan
    
    if len(p3) > 50 and 'P3_Accuracy' in p3.columns:
        p3['Time_Norm'] = (p3['Time(s)'] - p3['Time(s)'].min()) / (p3['Time(s)'].max() - p3['Time(s)'].min()) * 100
        p3['P3_Accuracy_Pct'] = p3['P3_Accuracy'] * 100 
        
        p3['Time_Bin'] = p3['Time_Norm'].round().astype(int)
        p3_binned = p3.groupby('Time_Bin')['P3_Accuracy_Pct'].mean().reset_index()
        p3_binned['Subject'] = p_label
        phase3_normalized = pd.concat([phase3_normalized, p3_binned])
        
        # Metric for the Slopegraph (First 20% vs Last 20%)
        acc_early = p3[p3['Time_Norm'] <= 20]['P3_Accuracy_Pct'].mean()
        acc_late = p3[p3['Time_Norm'] >= 80]['P3_Accuracy_Pct'].mean()

    results.append({
        'Subject': p_label,
        'Net_P0': net_0,
        'Net_P1_Early': net_1_early,
        'Net_P1_Late': net_1_late,
        'Net_P2': net_2,
        'Acc_Early': acc_early,
        'Acc_Late': acc_late
    })

df_res = pd.DataFrame(results).dropna(subset=['Net_P0'])

# ---------------------------------------------------------
# 3. HIGH-RESOLUTION GRAPH GENERATION
# ---------------------------------------------------------
print("\n--- GENERATING GRAPHS ---")

# --- G1: Evolution of Individuation ---
plt.figure(figsize=(10, 6))
phases = ['Phase 0', 'Early Phase 1', 'Late Phase 1', 'Phase 2']
means = [df_res['Net_P0'].mean(), df_res['Net_P1_Early'].mean(), df_res['Net_P1_Late'].mean(), df_res['Net_P2'].mean()]
sems = [df_res['Net_P0'].sem(), df_res['Net_P1_Early'].sem(), df_res['Net_P1_Late'].sem(), df_res['Net_P2'].sem()]

plt.plot(phases, means, marker='o', linewidth=3, markersize=10, color='#2c3e50')
plt.fill_between(phases, np.array(means) - np.array(sems), np.array(means) + np.array(sems), alpha=0.2, color='#3498db')
for i, txt in enumerate(means):
    plt.annotate(f"{txt:.2f} N", (phases[i], means[i] + 0.15), ha='center', fontweight='bold', fontsize=11)
plt.ylabel("Net Isolation Force (N)", fontweight='bold')
plt.title(f"Continuous Evolution of Individuation (N={len(df_res)})", fontweight='bold', fontsize=14)
plt.tight_layout()
plt.savefig("G1_Evolution_Individuation.png", dpi=300)
plt.close()

# --- G2: Boxplot P0 vs P2 ---
plt.figure(figsize=(8, 6))
df_melt = df_res.melt(id_vars=['Subject'], value_vars=['Net_P0', 'Net_P2'], var_name='Phase', value_name='Net_Force')
df_melt['Phase'] = df_melt['Phase'].replace({'Net_P0': 'Phase 0\n(Free)', 'Net_P2': 'Phase 2\n(Constrained)'})
sns.boxplot(x='Phase', y='Net_Force', data=df_melt, palette="Set2", width=0.5)
sns.stripplot(x='Phase', y='Net_Force', data=df_melt, color="black", alpha=0.5, size=6)
plt.ylabel("Net Isolation Force (N)", fontweight='bold')
plt.title("Dispersion of Net Isolation Force", fontweight='bold', fontsize=14)
plt.tight_layout()
plt.savefig("G2_Boxplot_P0_P2.png", dpi=300)
plt.close()

# --- G3: SLOPEGRAPH 16:9 (INTEGRATED) ---
df_acc_valid = df_res.dropna(subset=['Acc_Early', 'Acc_Late'])

if not df_acc_valid.empty:
    median_early = df_acc_valid['Acc_Early'].median()
    median_late = df_acc_valid['Acc_Late'].median()

    fig, ax = plt.subplots(figsize=(16, 9))

    # Individuallines
    for index, row in df_acc_valid.iterrows():
        ax.plot([1, 2], [row['Acc_Early'], row['Acc_Late']], 
                color='gray', alpha=0.3, linewidth=1.5, 
                label='Individual Subject' if index == 0 else "")

    # Median line
    ax.plot([1, 2], [median_early, median_late], 
            color='crimson', linewidth=4, marker='o', markersize=10, 
            label='Median Progression')

    # Formatting
    ax.set_title('Phase 3 Accuracy: Individual vs. Median Progression', fontsize=18, fontweight='bold', pad=20)
    ax.set_ylabel('Accuracy (%)', fontsize=14, fontweight='bold')
    ax.set_xticks([1, 2])
    ax.set_xticklabels(['First 20%', 'Last 20%'], fontsize=13, fontweight='bold')
    ax.set_xlim(0.8, 2.2)
    
    ax.grid(axis='y', linestyle='--', alpha=0.7)
    ax.legend(loc='upper left', fontsize=12)

    plt.tight_layout()
    plt.savefig('G3_Slopegraph_Phase3.png', dpi=300, bbox_inches='tight')
    plt.close()
    print(f"Slopegraph 16:9 généré pour {len(df_acc_valid)} sujets.")

# --- CHECK FOR OVERLAPS (DIAGNOSTIC) ---
doublons = df_acc_valid.duplicated(subset=['Acc_Early', 'Acc_Late'], keep=False)
if doublons.any():
    print("\n[Note] Des sujets ont des valeurs identiques et se superposent sur le graphique :")
    print(df_acc_valid[doublons][['Subject', 'Acc_Early', 'Acc_Late']])

print("\nSuccess: Tous les graphiques ont été générés.")