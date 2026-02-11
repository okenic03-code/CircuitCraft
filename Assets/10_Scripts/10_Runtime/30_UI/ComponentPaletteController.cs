using UnityEngine;
using UnityEngine.UIElements;
using CircuitCraft.Data;
using CircuitCraft.Controllers;
using System.Collections.Generic;
using System;

namespace CircuitCraft.UI
{
    /// <summary>
    /// UI Toolkit controller for the component palette.
    /// Manages button interactions to select components for placement.
    /// 
    /// Expected UXML structure:
    /// - VisualElement name="component-palette"
    ///   - Button name="btn-{component-id}" for each component (e.g. "btn-resistor-1k")
    /// </summary>
    public class ComponentPaletteController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] 
        [Tooltip("Reference to the UIDocument component.")]
        private UIDocument _uiDocument;

        [SerializeField] 
        [Tooltip("Reference to the PlacementController for handling component selection.")]
        private PlacementController _placementController;

        [Header("Configuration")]
        [SerializeField] 
        [Tooltip("List of component definitions available in the palette.")]
        private ComponentDefinition[] _componentDefinitions;
        
        // Store registered callbacks to properly unregister them
        private List<(Button btn, Action action)> _registeredCallbacks = new List<(Button, Action)>();
        
        private VisualElement _root;
        
        private void Awake()
        {
            // Validate dependencies
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
                if (_uiDocument == null)
                {
                    Debug.LogError("ComponentPaletteController: UIDocument reference is missing!");
                }
            }

            if (_placementController == null)
            {
                _placementController = FindFirstObjectByType<PlacementController>();
                if (_placementController == null)
                {
                    Debug.LogError("ComponentPaletteController: PlacementController reference is missing!");
                }
            }
        }

        private void OnEnable()
        {
            if (_uiDocument == null) return;

            _root = _uiDocument.rootVisualElement;
            if (_root == null) return;
            
            RegisterCallbacks();
        }
        
        private void OnDisable()
        {
            UnregisterCallbacks();
        }
        
        /// <summary>
        /// Finds buttons by name and registers click callbacks.
        /// </summary>
        private void RegisterCallbacks()
        {
            // Clear any existing callbacks just in case
            UnregisterCallbacks();

            if (_componentDefinitions == null) return;

            foreach (var def in _componentDefinitions)
            {
                if (def == null) continue;

                // Look for button with name format "btn-{id}"
                string buttonName = $"btn-{def.Id}";
                Button btn = _root.Q<Button>(buttonName);

                if (btn != null)
                {
                    // Create the action (captured closure)
                    Action action = () => OnComponentButtonClicked(def);
                    
                    // Register and track
                    btn.clicked += action;
                    _registeredCallbacks.Add((btn, action));
                }
                else
                {
                    // Optional: Log warning if button is missing for a definition, 
                    // but suppress for now as UXML might not have all defined components yet.
                    // Debug.LogWarning($"ComponentPaletteController: Button '{buttonName}' not found in UXML.");
                }
            }
        }

        /// <summary>
        /// Unregisters all tracked callbacks to prevent memory leaks or double-invocation.
        /// </summary>
        private void UnregisterCallbacks()
        {
            foreach (var (btn, action) in _registeredCallbacks)
            {
                if (btn != null)
                {
                    btn.clicked -= action;
                }
            }
            _registeredCallbacks.Clear();
        }
        
        /// <summary>
        /// Handler for component button clicks.
        /// Notifies PlacementController to select the component.
        /// </summary>
        private void OnComponentButtonClicked(ComponentDefinition def)
        {
            if (_placementController != null)
            {
                _placementController.SetSelectedComponent(def);
                
                // TODO: Add visual feedback (highlight selected button) if needed.
                // This could be handled via USS pseudo-states or manually setting classes.
            }
        }
    }
}
