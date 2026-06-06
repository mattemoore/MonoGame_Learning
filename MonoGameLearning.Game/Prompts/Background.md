# Background Generation Workflow

To generate a new background suitable for resizing into an 800x600 (4:3) aspect ratio without distorting perspective, follow these steps:

### 1. Image Generation Prompt

Use the following base prompt to generate a 1024x1024 square image that is deliberately **vertically stretched**:

```text
A background image for a sidescrolling beat em up game in the vein of Double Dragon and Final Fight. 16-bit pixel art graphics style. Pure flat side-scrolling perspective (camera looking straight at the buildings, perpendicular to the street) ideal for scrolling left to right, with a clear horizontal ground plane. No deep vanishing points. No characters or people in the image. 
LAYOUT RULES: The playable street area (from the bottom of the image to the front wall of the buildings) MUST take up exactly the bottom 40% of the image height. The buildings must start above this 40% mark. Ensure the far left and right edges feature generic walls/bricks (no important doors or windows cut in half), so a seam-hiding object can be placed over it later in-game. 
PROP RULE: The entire street/floor area MUST be completely bare and empty of any objects, crates, trash, or hydrants. Only draw the flat pavement texture. 
CRITICAL PROPORTION RULE: This 1024x1024 square image will later be compressed into an 800x600 display, flattening it vertically by 25%. To counteract this, you MUST draw everything vertically stretched (tall and skinny) by 33%. All doors, windows, bricks, and objects should appear unnaturally elongated and thin in this square image, so they return to perfect, normal proportions when vertically squished later.
```

*(Note: If you are generating a second subsequent background to logically continue the street, append this sentence to the prompt above):*

```text
THIS IMAGE MUST BE A LOGICAL CONTINUATION OF A STREET SCENE, KEEPING THE SAME STYLE AND PERSPECTIVE.
```

### 2. Resizing with FFmpeg

Once the stretched square image is generated and saved locally, use `ffmpeg` to compress the image vertically into its final 4:3 (800x600) resolution.

Ensure your terminal is in the directory containing the original image (or provide absolute paths), and run:

```bash
ffmpeg -y -i <input_image.png> -vf "scale=800:600" <output_image.png>
```
