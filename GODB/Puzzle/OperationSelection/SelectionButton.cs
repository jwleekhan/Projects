using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.Puzzle
{
    public struct SelectionButtonConfig
    {
        public readonly string text;
        public readonly Action<SelectionType, OperationSelection> onClick;
        public readonly SelectionType selectionType;
        public readonly OperationSelection operationSelection;

        public SelectionButtonConfig(string _text, Action<SelectionType, OperationSelection> _onClick,
                                        SelectionType _selectionType, OperationSelection _operationSelection)
        {
            text = _text;
            onClick = _onClick;
            selectionType = _selectionType;
            operationSelection = _operationSelection;
        }
    }

    public sealed class SelectionButton : MonoBehaviour
    {
        private OperationSelection _selection;
        public OperationSelection Selection => _selection;

        private SelectionButtonConfig _config;
        public SelectionButtonConfig Config => _config;

        [SerializeField] private Image imageComponent;
        [SerializeField] private Button buttonComponent;
        [SerializeField] private TMP_Text textComponent;

        public void Setup(SelectionButtonConfig config)
        {
            _selection = config.operationSelection;
            _config = config;

            textComponent.text = config.text;
            buttonComponent.onClick.RemoveAllListeners();
            buttonComponent.onClick.AddListener(() => config.onClick?.Invoke(config.selectionType, config.operationSelection));
        }
        
        public void SetImage(Sprite sprite, float colorAlpha)
        {
            imageComponent.sprite = sprite;
            Color color = imageComponent.color;
            color.a = colorAlpha;
            imageComponent.color = color;
        }
    }
}
