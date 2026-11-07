# Application Icon

- `netwatch-lite.png`: original transparent raster master generated with the built-in image generation tool on 2026-11-07.
- `netwatch-lite.ico`: derived Windows icon with 16, 24, 32, 48, 64, 128, and 256 pixel RGBA frames.
- `scripts/build-icon.ps1` converts/resamples the master without redesigning it. Run it from the repository root after an approved master replacement.

The symbol is a dashboard display with four panels. It represents the wallboard, not page-content alarms or scraping. The same ICO is used for the application executable, root portable launcher, main window, Settings, and Diagnostics. Native message boxes keep their standard information/warning/error symbols.

The application embeds the ICO; no loose asset is required for its windows to open. Each window owns a clone and disposes it when closed. Existing website favicons inside WebView2 are not changed. This is not a hosted website, so no web manifest or favicon route is added.

## Generation Prompt

Built-in image generation, transparent background, no reference image:

```text
Use case: logo-brand. Asset type: Windows desktop application icon and favicon-sized window symbol for NetWatch Lite Wallboard, an operational multi-dashboard display application. Create one original, centered, flat professional icon: a bold charcoal computer display frame with a short stand, showing a very simple 2 by 2 grid of dashboard panels. Three panels in clean teal/cyan shades and one muted amber panel; broad clean negative-space gutters. Strong geometric silhouette, solid fills, crisp edges, no tiny UI content. Flat front-facing view, not isometric. The design must remain recognizable when reduced to 16x16 and 32x32 pixels. Square 1024 by 1024 canvas, generous 8 percent transparent margin, symbol occupies most of canvas. Truly transparent background outside the symbol. No text, no letters, no numbers, no watermark, no photography, no glow, no gradients, no shadows, no 3D, no alarm bells, no heartbeat line. Supply a single icon only, not a sheet of alternatives.
```
