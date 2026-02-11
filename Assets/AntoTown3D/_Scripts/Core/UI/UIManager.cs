using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Build Panel")]
    public GameObject buildPanel;

    [Header("Build Category Panels")]
    public GameObject residentPanel;
    public GameObject commercialPanel;
    public GameObject industryPanel;
    public GameObject powerPanel;
    public GameObject waterPanel;
    public GameObject parkPanel;

    void Start()
    {
        buildPanel.SetActive(false);
        HideAllCategoryPanels();
    }

    // ===== BUILD PANEL =====
    public void OpenBuildPanel()
    {
        buildPanel.SetActive(true);
        ShowResidentPanel(); // default
    }

    public void CloseBuildPanel()
    {
        buildPanel.SetActive(false);
        HideAllCategoryPanels();
    }

    // ===== CATEGORY BUTTONS =====
    public void ShowResidentPanel()
    {
        HideAllCategoryPanels();
        residentPanel.SetActive(true);
    }

    public void ShowCommercialPanel()
    {
        HideAllCategoryPanels();
        commercialPanel.SetActive(true);
    }

    public void ShowIndustryPanel()
    {
        HideAllCategoryPanels();
        industryPanel.SetActive(true);
    }

    public void ShowPowerPanel()
    {
        HideAllCategoryPanels();
        powerPanel.SetActive(true);
    }

    public void ShowWaterPanel()
    {
        HideAllCategoryPanels();
        waterPanel.SetActive(true);
    }

    public void ShowParkPanel()
    {
        HideAllCategoryPanels();
        parkPanel.SetActive(true);
    }

    // ===== HELPER =====
    public void HideAllCategoryPanels()
    {
        residentPanel.SetActive(false);
        commercialPanel.SetActive(false);
        industryPanel.SetActive(false);
        powerPanel.SetActive(false);
        waterPanel.SetActive(false);
        parkPanel.SetActive(false);
    }

}
