The previous generator build_collectible_pin.py is preserved. Prefer it over build_asset.py for the working pin mesh and 11 mm coffer socket fit.
# Installed detailed source binding

The authoritative editable model is `Assets/Objects/Pin.blend`, used by `Assets/DetectiveDesk/Prefabs/CollectiblePin.prefab`. Its embedded `build_collectible_pin.py` exactly matches the original kit in Downloads. An unchanged copy is preserved here as `build_collectible_pin.py`. Prefer that detailed source over the generic `build_asset.py`. Before a requested rebuild, adapt its output handling and source embedding while preserving the current socket dimensions, enamel renderer, brass materials, PinSkin component and model pivot. Do not run the generic starter against the completed pin.

