using UnityEngine;

namespace Corebound.Input
{
    [CreateAssetMenu(menuName = "Corebound/Input/Controller Glyph Data", fileName = "ControllerGlyphData")]
    public sealed class ControllerGlyphData : ScriptableObject
    {
        [SerializeField] private string _confirmGlyphKey = "confirm";
        [SerializeField] private string _cancelGlyphKey = "cancel";
        [SerializeField] private string _interactGlyphKey = "interact";
        [SerializeField] private string _moveGlyphKey = "move";
        [SerializeField] private string _fireGlyphKey = "fire";
        [SerializeField] private string _confirmGamepadLabel = "A";
        [SerializeField] private string _confirmKeyboardLabel = "Enter";
        [SerializeField] private string _cancelGamepadLabel = "B";
        [SerializeField] private string _cancelKeyboardLabel = "Esc";
        [SerializeField] private string _interactGamepadLabel = "X";
        [SerializeField] private string _interactKeyboardLabel = "E";
        [SerializeField] private string _moveGamepadLabel = "Left Stick";
        [SerializeField] private string _moveKeyboardLabel = "WASD";
        [SerializeField] private string _fireGamepadLabel = "RT";
        [SerializeField] private string _fireKeyboardLabel = "LMB";

        public string ConfirmGlyphKey => _confirmGlyphKey;

        public string CancelGlyphKey => _cancelGlyphKey;

        public string InteractGlyphKey => _interactGlyphKey;

        public string MoveGlyphKey => _moveGlyphKey;

        public string FireGlyphKey => _fireGlyphKey;

        public string GetConfirmLabel(bool isUsingGamepad)
        {
            return isUsingGamepad ? _confirmGamepadLabel : _confirmKeyboardLabel;
        }

        public string GetCancelLabel(bool isUsingGamepad)
        {
            return isUsingGamepad ? _cancelGamepadLabel : _cancelKeyboardLabel;
        }

        public string GetInteractLabel(bool isUsingGamepad)
        {
            return isUsingGamepad ? _interactGamepadLabel : _interactKeyboardLabel;
        }

        public string GetMoveLabel(bool isUsingGamepad)
        {
            return isUsingGamepad ? _moveGamepadLabel : _moveKeyboardLabel;
        }

        public string GetFireLabel(bool isUsingGamepad)
        {
            return isUsingGamepad ? _fireGamepadLabel : _fireKeyboardLabel;
        }
    }
}