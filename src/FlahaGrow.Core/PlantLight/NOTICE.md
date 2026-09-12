# CIE photopic weighting data

FlahaGrow.Core embeds the unchanged CIE spectral luminous efficiency for
photopic vision CSV and its metadata. Publisher: International Commission on
Illumination (CIE), Vienna, Austria, 2019.

Source: https://cie.co.at/datatable/cie-spectral-luminous-efficiency-photopic-vision
DOI: https://doi.org/10.25039/CIE.DS.dktna2s3
License: Creative Commons Attribution-ShareAlike 4.0 International,
https://creativecommons.org/licenses/by-sa/4.0/

No CIE endorsement is implied. Original data and metadata are retained under
docs/research/plant-light/data/cie. This notice must accompany distributions
of the assembly. See the research README for the landing-page checksum
discrepancy; embedded bytes match the metadata's SHA-256 and MD5.

The assembly also embeds pre-integrated reference factors from
docs/research/plant-light/profile-audit.json (research-linear-1nm-trapezoid-v1).
CIE daylight D55/D65/D75 and nine LED factors are adaptations of CIE datasets
under CC BY-SA 4.0. Dataset DOIs and source SHA256 hashes accompany every
runtime profile. The CIE-derived factor data remain under CC BY-SA 4.0;
this does not relicense unrelated plugin code.

Six horticultural treatment factors derive from Yujin Park and Erik S. Runkle
(2018), https://doi.org/10.1371/journal.pone.0202386, supplementary data
https://doi.org/10.6084/m9.figshare.6946136.v1, under CC BY 4.0,
https://creativecommons.org/licenses/by/4.0/ . Adaptation: photon-basis spectra
integrated into PPFD/lux ratios with zero unmeasured photopic tails.
These are research treatments, not certified commercial fixture presets.

All library selections require explicit acknowledgment of displayed limitations.
