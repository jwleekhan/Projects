using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Puzzle
{
    public sealed class OperationSelectionView : MonoBehaviour
    {
        [Header("Selection")]
        [SerializeField] private Button btnUndo;
        [SerializeField] private Button btnRedo;
        [SerializeField] private ScrollRect scrollViewTarget;
        [SerializeField] private ScrollRect scrollViewAction;
        [SerializeField] private ScrollRect scrollViewExtra;
        [SerializeField] private Transform btnTargetGroup;
        [SerializeField] private Transform btnActionGroup;
        [SerializeField] private Transform btnExtraGroup;
        [SerializeField] private SelectionButton selectedBtnTarget;
        [SerializeField] private SelectionButton selectedBtnAction;
        [SerializeField] private SelectionButton selectedBtnExtra;
        [SerializeField] private Image imageAction;

        [Header("Resources")]
        [SerializeField] private SelectionButton selectionBtnPrefab;
        [SerializeField] private Sprite selectionDefaultSprite;
        [SerializeField] private Sprite selectionSelectedSprite;
        [SerializeField] private Sprite selectionNotSelectedSprite;
        [SerializeField] private Sprite actionMoveSprite;
        [SerializeField] private Sprite actionInterchangeSprite;
        [SerializeField] private Sprite actionChangeToSprite;
        [SerializeField] private Sprite actionDeleteSprite;

        [Header("Apply")]
        [SerializeField] private Button btnApply;
        [SerializeField] private Image imageArrow;
        [SerializeField] private Sprite applyDisabledSprite;
        [SerializeField] private Sprite apply1LineSprite;
        [SerializeField] private Sprite apply2LineSprite;
        [SerializeField] private GameObject imageApplyFail;
        [SerializeField] private float failPopupDuration = 2f;
        [SerializeField] private Button btnSubmit;

        private void Awake()
        {
            btnApply.GetComponentInChildren<TMP_Text>().text = "";
            imageApplyFail.SetActive(false);
        }

        private void SetBtnInteractable(Button btn, bool interactable)
        {
            if (btn.interactable == interactable) return;
            btn.interactable = interactable;
            if (btn.TryGetComponent<Image>(out var image))
            {
                Color color = image.color;
                color.a = interactable ? 1 : 0.3f;
                image.color = color;
            }
        }

        public void SetUndoInteractable(bool interactable)
            => SetBtnInteractable(btnUndo, interactable);

        public void SetRedoInteractable(bool interactable)
            => SetBtnInteractable(btnRedo, interactable);

        public void SetSubmitInteractable(bool interactable)
            => SetBtnInteractable(btnSubmit, interactable);

        public void InitSelectionButtons(List<SelectionButtonConfig> targetButtonConfigs, List<SelectionButtonConfig> actionButtonConfigs, List<SelectionButtonConfig> extraButtonConfigs)
        {
            InitButtonsOfType(SelectionType.Target, targetButtonConfigs);
            InitButtonsOfType(SelectionType.Action, actionButtonConfigs);
            InitButtonsOfType(SelectionType.Extra, extraButtonConfigs);
        }

        private void InitButtonsOfType(SelectionType selectionType, List<SelectionButtonConfig> buttonConfigs)
        {
            if (selectionType == SelectionType.None) return;

            Clear(selectionType);

            Transform parent = GetParent(selectionType);

            foreach (var config in buttonConfigs)
            {
                SelectionButton btn = Instantiate(selectionBtnPrefab, parent);
                btn.Setup(config);
                btn.gameObject.SetActive(false);
            }
        }

        private void Clear(SelectionType selectionType = SelectionType.None)
        {
            if (selectionType == SelectionType.None)
            {
                Clear(SelectionType.Target);
                Clear(SelectionType.Action);
                Clear(SelectionType.Extra);
                return;
            }

            Transform parent = GetParent(selectionType);

            foreach (Transform child in parent)
            {
                Destroy(child.gameObject);
            }

            ResetScroll(selectionType);

            if (selectionType == SelectionType.Action)
                SetImageAction(ActionType.None);
            UpdateSelected(selectionType, false, default);
        }

        public void UpdateSelectionButtons(OperationSelection selection, Predicate<SelectionButton> canSelect)
        {
            UpdateButtonsOfType(SelectionType.Target, selection, canSelect);
            UpdateButtonsOfType(SelectionType.Action, selection, canSelect);
            UpdateButtonsOfType(SelectionType.Extra, selection, canSelect);
            SetImageAction(selection.action);
        }

        private void UpdateButtonsOfType(SelectionType selectionType, OperationSelection selection, Predicate<SelectionButton> canSelect)
        {
            if (selectionType == SelectionType.None) return;

            // Selected Panel에 표시된 버튼을 숨김
            UpdateSelected(selectionType, false, default);
            
            Transform parent = GetParent(selectionType);
            bool isChanged = false;

            // 타입에 맞는 모든 선택 버튼 순회
            foreach (Transform child in parent)
            {
                if (!child.TryGetComponent<SelectionButton>(out var btn))
                    continue;

                bool prevActiveSelf = btn.gameObject.activeSelf;
                // 선택 가능한 버튼이 아니면 숨김
                if (!canSelect(btn))
                {
                    btn.gameObject.SetActive(false);
                    if (prevActiveSelf != btn.gameObject.activeSelf)
                        isChanged = true;
                    continue;
                }
                btn.gameObject.SetActive(true);
                if (prevActiveSelf != btn.gameObject.activeSelf)
                    isChanged = true;

                // 아직 선택된 것이 없다면 모든 버튼에 기본 스프라이트 할당
                if (PuzzleLogic.IsSameSelection(selectionType, selection, default))
                {
                    SetSpriteDefault(btn);
                    continue;
                }

                // 선택된 것이 이 버튼과 일치하는지에 따라 스프라이트 할당, 일치한다면 Selected Panel에도 표시
                if (PuzzleLogic.IsSameSelection(selectionType, selection, btn.Selection))
                {
                    SetSpriteSelectedOrNot(btn, true);
                    UpdateSelected(selectionType, true, btn.Config);
                }
                else
                {
                    SetSpriteSelectedOrNot(btn, false);
                }
            }

            // 버튼의 활성화 상태가 변한 것이 있다면 스크롤을 초기화
            if (isChanged)
                ResetScroll(selectionType);
        }

        private void SetSpriteDefault(SelectionButton btn)
        {
            btn.SetImage(selectionDefaultSprite, 1);
        }

        private void SetSpriteSelectedOrNot(SelectionButton btn, bool isSelected)
        {
            if (isSelected)
            {
                btn.SetImage(selectionSelectedSprite, 1);
            }
            else
            {
                btn.SetImage(selectionNotSelectedSprite, 0.7f);
            }
        }

        private void SetImageAction(ActionType action)
        {
            switch (action)
            {
                case ActionType.Move:
                    imageAction.sprite = actionMoveSprite;
                    break;
                case ActionType.Interchange:
                    imageAction.sprite = actionInterchangeSprite;
                    break;
                case ActionType.ChangeTo:
                    imageAction.sprite = actionChangeToSprite;
                    break;
                case ActionType.Delete:
                    imageAction.sprite = actionDeleteSprite;
                    break;
                default:
                    imageAction.gameObject.SetActive(false);
                    return;
            }

            imageAction.gameObject.SetActive(true);
        }

        private void UpdateSelected(SelectionType selectionType, bool isSelected, SelectionButtonConfig config)
        {
            if (selectionType == SelectionType.None)
                return;

            SelectionButton btn = GetSelected(selectionType);

            if (!isSelected)
            {
                btn.gameObject.SetActive(false);
                return;
            }

            btn.Setup(config);
            btn.gameObject.SetActive(true);
        }

        public void UpdateApply(bool isActive, string text)
        {
            btnApply.interactable = isActive;
            imageArrow.gameObject.SetActive(isActive);
            if (isActive)
            {
                TMP_Text btnText = btnApply.GetComponentInChildren<TMP_Text>();
                btnText.text = text;
                btnText.ForceMeshUpdate();
                Sprite sprite = (btnText.textInfo.lineCount > 1) ? apply2LineSprite : apply1LineSprite;
                btnApply.GetComponent<Image>().sprite = sprite;
            }
            else
            {
                btnApply.GetComponentInChildren<TMP_Text>().text = "";
                btnApply.GetComponent<Image>().sprite = applyDisabledSprite;
            }
        }

        public void ShowImageApplyFail()
        {
            StopCoroutine(nameof(ShowImageApplyFailCoroutine));
            StartCoroutine(nameof(ShowImageApplyFailCoroutine));
        }

        private IEnumerator ShowImageApplyFailCoroutine()
        {
            imageApplyFail.SetActive(true);
            yield return new WaitForSeconds(failPopupDuration);
            imageApplyFail.SetActive(false);
        }

        public void ResetScroll(SelectionType selectionType = SelectionType.None)
        {
            if (selectionType == SelectionType.None)
            {
                ResetScroll(SelectionType.Target);
                ResetScroll(SelectionType.Action);
                ResetScroll(SelectionType.Extra);
                return;
            }

            GetScrollRect(selectionType).verticalNormalizedPosition = 1f;
        }

        public void OnClickScrollTarget(float amount)
        {
            ScrollByPixels(SelectionType.Target, amount);
        }

        public void OnClickScrollExtra(float amount)
        {
            ScrollByPixels(SelectionType.Extra, amount);
        }

        private void ScrollByPixels(SelectionType selectionType, float pixelAmount)
        {
            if (selectionType == SelectionType.None)
                return;

            ScrollRect scrollRect = GetScrollRect(selectionType);

            RectTransform content = scrollRect.content;
            RectTransform viewport = scrollRect.viewport;

            float contentHeight = content.rect.height;
            float viewportHeight = viewport.rect.height;

            float maxScroll = contentHeight - viewportHeight;

            Vector2 pos = content.anchoredPosition;
            pos.y = Mathf.Clamp(pos.y + pixelAmount, 0, maxScroll);

            content.anchoredPosition = pos;
        }

        private ScrollRect GetScrollRect(SelectionType selectionType)
        {
            return selectionType switch
            {
                SelectionType.Target => scrollViewTarget,
                SelectionType.Action => scrollViewAction,
                SelectionType.Extra => scrollViewExtra,
                _ => null
            };
        }

        private Transform GetParent(SelectionType selectionType)
        {
            return selectionType switch
            {
                SelectionType.Target => btnTargetGroup,
                SelectionType.Action => btnActionGroup,
                SelectionType.Extra => btnExtraGroup,
                _ => null
            };
        }

        private SelectionButton GetSelected(SelectionType selectionType)
        {
            return selectionType switch
            {
                SelectionType.Target => selectedBtnTarget,
                SelectionType.Action => selectedBtnAction,
                SelectionType.Extra => selectedBtnExtra,
                _ => null
            };
        }
    }
}

