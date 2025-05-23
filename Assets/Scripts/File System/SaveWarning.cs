using System;
using UnityEngine;
using UnityEngine.UI;

public class SaveWarning : MonoBehaviour
{
    private FileSystem fileSystem;

    // Refer�ncia ao painel de aviso
    public GameObject warningPanel;
    public Button saveButton;
    public Button dontSaveButton;

    // Fun��es de salvar e n�o salvar
    private Action onSave;
    private Action onDontSave;

    private void Awake()
    {
        fileSystem = FindAnyObjectByType<FileSystem>();
    }

    // Chama para exibir o aviso com a��es passadas como par�metros
    public void ShowUnsavedChangesWarning(Action onSave, Action onDontSave)
    {
        this.onSave = onSave;
        this.onDontSave = onDontSave;

        // Exibe o painel de aviso
        warningPanel.SetActive(true);

        // Define as fun��es de callback para os bot�es
        saveButton.onClick.RemoveAllListeners(); // Limpa listeners anteriores
        saveButton.onClick.AddListener(() =>
        {
            onSave?.Invoke(); // Chama a fun��o de salvar
            HideWarning(); // Esconde o painel
        });

        dontSaveButton.onClick.RemoveAllListeners(); // Limpa listeners anteriores
        dontSaveButton.onClick.AddListener(() =>
        {
            onDontSave?.Invoke(); // Chama a fun��o de n�o salvar
            HideWarning(); // Esconde o painel
        });
    }

    // Esconde o painel de aviso
    public void HideWarning()
    {
        warningPanel.SetActive(false);
    }
}
