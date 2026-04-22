import os
import glob
import numpy as np
import pandas as pd
from scipy import stats

# Configuration
FORCE_COLS = ['Force_F0', 'Force_F1', 'Force_F2', 'Force_F3', 'Force_F4']

def calc_net_force(row):
    forces = sorted([row[c] for c in FORCE_COLS], reverse=True)
    return forces[0] - forces[1]

csv_files = glob.glob("*.csv")
results = []

print(f"--- ANALYSE DE {len(csv_files)} FICHIERS CSV ---")

for filepath in csv_files:
    raw_name = os.path.basename(filepath).split('_')[0]
    df = pd.read_csv(filepath)
    df.columns = df.columns.str.strip()
    
    df['Net_Force'] = df.apply(calc_net_force, axis=1)
    
    # Filtrer l'activité
    df_active = df[df[FORCE_COLS].max(axis=1) > 0.5].copy()
    
    p0 = df_active[df_active['Phase'] == 0]
    p2 = df_active[df_active['Phase'] == 2]
    p3 = df[df['Phase'] == 3].copy()
    
    net_0 = p0['Net_Force'].mean() if not p0.empty else np.nan
    net_2 = p2['Net_Force'].mean() if not p2.empty else np.nan
    
    acc_mean = np.nan
    acc_early = np.nan
    acc_late = np.nan
    
    if len(p3) > 50 and 'P3_Accuracy' in p3.columns:
        p3['Time_Norm'] = (p3['Time(s)'] - p3['Time(s)'].min()) / (p3['Time(s)'].max() - p3['Time(s)'].min()) * 100
        p3['P3_Accuracy_Pct'] = p3['P3_Accuracy'] * 100
        
        acc_mean = p3['P3_Accuracy_Pct'].mean()
        acc_early = p3[p3['Time_Norm'] <= 20]['P3_Accuracy_Pct'].mean()
        acc_late = p3[p3['Time_Norm'] >= 80]['P3_Accuracy_Pct'].mean()

    results.append({
        'Sujet': raw_name,
        'Net_P0': net_0,
        'Net_P2': net_2,
        'Acc_Mean': acc_mean,
        'Acc_Early': acc_early,
        'Acc_Late': acc_late
    })

df_res = pd.DataFrame(results).dropna(subset=['Net_P0', 'Net_P2'])

# --- CALCULS STATISTIQUES (Approche Non-Paramétrique Exclusive) ---

print("\n==================================================")
print(" 1. RÉSULTATS : INDIVIDUATION (Force Nette P0 vs P2)")
print("==================================================")
net_p0 = df_res['Net_P0'].values
net_p2 = df_res['Net_P2'].values
diff_net = net_p2 - net_p0

mean_p0, mean_p2 = np.mean(net_p0), np.mean(net_p2)

# Test de Wilcoxon assumé (N faible)
stat_w_net, p_val_net = stats.wilcoxon(net_p0, net_p2)
cohen_d_net = np.mean(diff_net) / np.std(diff_net, ddof=1)

print(f"Moyenne Phase 0 : {mean_p0:.2f} N")
print(f"Moyenne Phase 2 : {mean_p2:.2f} N")
print(f"Progression     : +{(mean_p2 - mean_p0):.2f} N")
print(f"Test utilisé    : Test des rangs signés de Wilcoxon")
print(f"P-value         : {p_val_net:.4e} (Si < 0.05 = Significatif)")
print(f"Cohen's d       : {cohen_d_net:.2f} (Taille d'effet : >0.8 = Fort)")

df_acc = df_res.dropna(subset=['Acc_Early', 'Acc_Late'])
print("\n==================================================")
print(" 2. RÉSULTATS : CONVERGENCE (Précision Phase 3)")
print("==================================================")
if not df_acc.empty:
    acc_early = df_acc['Acc_Early'].values
    acc_late = df_acc['Acc_Late'].values
    diff_acc = acc_late - acc_early
    
    stat_w_acc, p_val_acc = stats.wilcoxon(acc_early, acc_late)
    cohen_d_acc = np.mean(diff_acc) / np.std(diff_acc, ddof=1)

    print(f"Accuracy Moyenne Globale : {df_acc['Acc_Mean'].mean():.1f}%")
    print(f"Accuracy Début (20%)     : {np.mean(acc_early):.1f}%")
    print(f"Accuracy Fin (20%)       : {np.mean(acc_late):.1f}%")
    print(f"Test utilisé             : Test des rangs signés de Wilcoxon")
    print(f"P-value                  : {p_val_acc:.4e}")
    print(f"Cohen's d                : {cohen_d_acc:.2f}")
else:
    print("Pas assez de données valides pour la Phase 3.")
print("==================================================\n")