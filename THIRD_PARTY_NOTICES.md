# THIRD PARTY NOTICES

This project references design direction from:

- kushal-goenka/VRTableTennis (table tennis gameplay structure, resource inspiration)
- tomgoddard/PingPang (future hit/velocity physics inspiration)
- Pico-Developer/InteractionSample-Unity (PICO interaction patterns)

Notes:
- This demo keeps existing PICO project configuration and does not import Oculus/SteamVR/Meta XR stacks.
- Selected VRTableTennis assets have been copied into `Assets/_Project/External/VRTableTennis/Original` for local demo reuse: paddle/table/ball FBX files, table materials/texture, and ping-pong audio clips.
- Cleaned prefabs are stored in `Assets/_Project/External/VRTableTennis/Adapted` and exclude old XR/Oculus/SteamVR/Photon scripts.
- The upstream VRTableTennis repository is MIT licensed; its license text is preserved at `Assets/_Project/External/VRTableTennis/Original/LICENSE`.
- Several copied FBX `.meta` files declare `licenseType: Store`; confirm the original asset-pack license before redistributing builds or source packages outside internal demo use.
- The MR rehab UI bundles `NotoSansSC-VF.ttf` at `Assets/_Project/Fonts/Rehab/` so TextMeshPro can render Simplified Chinese on device. Noto Sans SC is part of Google Noto Fonts and is licensed under the SIL Open Font License 1.1.
