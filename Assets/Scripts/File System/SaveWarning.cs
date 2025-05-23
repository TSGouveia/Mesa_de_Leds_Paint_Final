using System;
using UnityEngine;
using UnityEngine.UI;

public class SaveWarning : MonoBehaviour
{
    private FileSystem fileSystem;

    // Referência ao painel de aviso
    public GameObject warningPanel;
    public Button saveButton;
    public Button dontSaveButton;

    // Funções de salvar e não salvar
    private Action onSave;
    private Action onDontSave;

    private void Awake()
    {
        fileSystem = FindAnyObjectByType<FileSystem>();
    }

    // Chama para exibir o aviso com ações passadas como parâmetros
    public void ShowUnsavedChangesWarning(Action onSave, Action onDontSave)
    {
        this.onSave = onSave;
        this.onDontSave = onDontSave;

        // Exibe o painel de aviso
        warningPanel.SetActive(true);

        // Define as funções de callback para os botões
        saveButton.onClick.RemoveAllListeners(); // Limpa listeners anteriores
        saveButton.onClick.AddListener(() =>
        {
            onSave?.Invoke(); // Chama a função de salvar
            HideWarning(); // Esconde o painel
        });

        dontSaveButton.onClick.RemoveAllListeners(); // Limpa listeners anteriores
        dontSaveButton.onClick.AddListener(() =>
        {
            onDontSave?.Invoke(); // Chama a função de não salvar
            HideWarning(); // Esconde o painel
        });
    }

    // Esconde o painel de aviso
    public void HideWarning()
    {
        warningPanel.SetActive(false);
    }
}
