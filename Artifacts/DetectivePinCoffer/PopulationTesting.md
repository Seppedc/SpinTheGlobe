# Testing pin population

1. Enter Play Mode in Unity.
2. Choose Tools > Detective Desk > Test Pins > Populate 12 Skins.
3. Choose Validate Population to check all twelve seated pins and their independent skins.
4. Choose Clear Test Pins to remove only pins created by this helper, or stop Play Mode to discard the test session.

Populate fills empty sockets only. Running it twice does not duplicate pins. Existing occupants are retained. Validation expects Ruby through Midnight in slots 01 through 12 respectively; a pre-existing pin with a different skin will be reported as an unexpected skin.

The helper is editor-only: Assets/DetectiveDesk/Scripts/Editor/PinCofferPopulationTools.cs. It does not save a populated scene or alter the coffer prefab.

Automated validation passed: all twelve skins, independent renderers, seated transforms, duplicate prevention, and preservation of existing occupants during populate/clear. See PopulationValidation.txt and PopulatedPins_GameCamera.png. Mouse selection through native physics remains subject to the previously reported project physics issue; this test verifies population and appearance.
