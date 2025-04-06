using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum CreditsCategories
{
    Audio,
    Art,
    QA,
    Animation,
    Plugins,
    Other
}


[CreateAssetMenu(fileName = "SOCredit", menuName = "SO Credit/SOCredit")]
public class SOCredit : ScriptableObject
{
    [Header("Credit Info")]
    [SerializeField] private CreditsCategories creditCategory;
    [SerializeField] private string contributionDescription = "Description";
    [SerializeField] private string contributorName = "Name";
    
    public string ContributorName => contributorName;
    public string ContributionDescription => contributionDescription;
    public CreditsCategories CreditCategory => creditCategory;
    public string CreditString => $"{contributionDescription} - {contributorName}";

    
        
#if UNITY_EDITOR
    private void OnValidate()
    {
        // Use EditorApplication.delayCall to avoid calling during import
        EditorApplication.delayCall += () =>
        {
            // Check if this object still exists when the delayed call executes
            if (this == null) return;
            
            string assetPath = AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(assetPath) && !string.IsNullOrEmpty(contributorName))
            {
                string newName = $"Credit_{creditCategory}_{contributorName.Replace(" ", "")}_{contributionDescription.Replace(" ", "")}";
                // Only rename if the name has actually changed
                if (!assetPath.Contains(newName))
                {
                    AssetDatabase.RenameAsset(assetPath, newName);
                }
            }
        };
    }
#endif
}