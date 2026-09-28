"""Summarize selected lighting/environment fields from inspector FULL_MONO dumps."""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path


ENV_NAME = re.compile(r'^\tstring m_Name = "(Env_map01_lv006[^"]*)"$')
CONFIG = re.compile(r"^\t(\w+Config) (\w+)$")
VALUE = re.compile(r"^\t\t(?:UInt8|int|unsigned int|float|string) (\w+) = (.+)$")
CHILD = re.compile(r"^\t\t(?:\w+(?:<[^>]+>)?) (\w+)$")
CHILD_VALUE = re.compile(r"^\t\t\t(?:UInt8|int|unsigned int|float|string|SInt64) ([\w\[\] <>]+) = (.+)$")

SELECT = {
    "lightConfig": {
        "directColorMode", "directColorTemperature", "directLux", "directEV100",
        "directSpecularIntensity", "directSoftSourceRadius", "indirectDiffuseFactor",
        "indirectSpecularFactor", "indirectSpecularFactorType", "atmospherePitchYawMode",
        "lightShaftPitchYawMode", "sunDiscPitchYawMode", "m_active",
    },
    "skyConfig": {
        "skyDistance", "skyBakedIndirectIntensity", "skyDirectIntensity",
        "useCustomIVDefaultSH", "skyMaterialType", "proceduralSkyLuxFactor",
        "enableSunDisc", "sunDiscRadius", "sunDiscRampIntensity", "skyboxBrightness",
        "skyboxRotation", "reflectionType", "culloff", "m_active",
    },
    "fogConfig": {
        "enable", "startDistance", "startHeight", "fallOffHeight", "fallOffDistance",
        "mieScatteringScale", "mieAnisotropy", "rayleighScatteringScale", "m_active",
    },
    "heightFogConfig": {
        "enable", "heightFogStartHeight", "heightFogDensity", "heightFogFalloff",
        "heightFogStartHeightSecond", "heightFogDensitySecond", "heightFogFalloffSecond",
        "heightFogMaxOpacity", "heightFogStartDistance", "enableVolumetricFog",
        "volumetricFogScatteringDistribution", "volumetricFogExtinctionScale",
        "volumetricFogDistance", "volumetricFogStartDistance",
        "volumetricFogDirectLightingScatteringIntensity",
        "volumetricFogSkyLightingScatteringIntensity", "enableFlowNoise", "m_active",
    },
    "volumetricFogConfig": {
        "enable", "heightFogStartHeight", "heightFogDensity", "heightFogFalloff",
        "heightFogStartHeightSecond", "heightFogDensitySecond", "heightFogFalloffSecond",
        "heightFogMaxOpacity", "heightFogStartDistance",
        "volumetricFogScatteringDistribution", "volumetricFogExtinctionScale",
        "volumetricFogDistance", "volumetricFogStartDistance",
        "volumetricFogDirectLightingScatteringIntensity",
        "volumetricFogSkyLightingScatteringIntensity", "enableFlowNoise", "m_active",
    },
    "lightShaftConfig": {
        "enable", "bloomScale", "bloomThreshold", "bloomMaxBrightness",
        "blurIntensity", "enableOcclusion", "m_active",
    },
    "colorGradingConfig": {
        "tonemappingMode", "colorLookupEnabled", "colorLookupContribution",
        "whiteBalanceEnabled", "whiteBalanceTemperature", "whiteBalanceTint",
        "colorAdjustmentsEnabled", "colorAdjustmentsPostExposure",
        "colorAdjustmentsContrast", "colorAdjustmentsHueShift",
        "colorAdjustmentsSaturation", "m_active",
    },
    "autoExposureConfig": {
        "autoExposureMode", "autoExposureManualEvCompensationAuto",
        "autoExposureLerpUpSpeed", "autoExposureLerpDownSpeed",
        "autoExposureMeteringMode", "autoExposureManualEvCompensationManual", "m_active",
    },
    "shadowConfig": {
        "csmDepthBiasV2", "csmNormalBiasV2", "csmIntensity", "csmShadowSoftness",
        "contactShadowIntensity", "contactShadowSurfaceThickness", "disableCsm",
        "disableAsm", "m_active",
    },
}

SELECT_CHILD = {
    "lightConfig": {"directColor", "directCustomColor", "directPitchYaw"},
    "skyConfig": {"skyboxTintColor", "reflectionMap", "skyboxCubeMap"},
    "fogConfig": {"mieScattering", "rayleighScattering", "inscatterAmbientColor"},
    "heightFogConfig": {"heightFogInscatter", "volumetricFogAlbedo", "volumetricFogEmissive"},
    "volumetricFogConfig": {"heightFogInscatter", "volumetricFogAlbedo", "volumetricFogEmissive"},
    "lightShaftConfig": {"bloomTint"},
    "colorGradingConfig": {"colorAdjustmentsColorFilter", "colorLookupTexture"},
    "autoExposureConfig": {"autoExposureEv100Range", "autoExposureEv100HistogramRange", "autoExposurePixelLuminanceRange", "autoExposureEvClampRange"},
}


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("dump", type=Path)
    args = parser.parse_args()
    lines = args.dump.read_text(encoding="utf-8-sig", errors="replace").splitlines()

    result: dict[str, dict[str, dict[str, str]]] = {}
    env: str | None = None
    config: str | None = None
    child: str | None = None
    for line in lines:
        match = ENV_NAME.match(line)
        if match:
            env = match.group(1)
            result[env] = {}
            config = child = None
            continue
        match = CONFIG.match(line)
        if match:
            config = match.group(2) if env and match.group(2) in SELECT else None
            child = None
            if config:
                result[env][config] = {}
            continue
        if not env or not config:
            continue
        match = VALUE.match(line)
        if match:
            child = None
            key, value = match.groups()
            if key in SELECT[config]:
                result[env][config][key] = value
            continue
        match = CHILD.match(line)
        if match:
            candidate = match.group(1)
            child = candidate if candidate in SELECT_CHILD.get(config, set()) else None
            continue
        match = CHILD_VALUE.match(line)
        if match and child:
            key, value = match.groups()
            result[env][config][f"{child}.{key.strip()}"] = value

    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
