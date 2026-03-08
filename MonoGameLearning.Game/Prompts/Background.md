# Background Generation Workflow

To generate a new background suitable for resizing into an 800x600 (4:3) aspect ratio without distorting perspective, follow these steps:

### 1. Image Generation Prompt

Use the following base prompt to generate a 1024x1024 square image that is deliberately **vertically stretched**:

```text
A background image suitable to be used as a background for a sidescrolling beat em up game in the vein of Double Dragon and Final Fight. 16-bit pixel art graphics style. No props overlapping with the street. No characters or people in the image. IMPORTANT: Draw all elements vertically stretched (taller and thinner than normal) so that when this square image is later resized/squished into an 800x600 (4:3) aspect ratio, the perspective and proportions will finally look correct.
```

*(Note: If you need to naturally extend an existing background to the right so they seamlessly scroll together, you MUST provide the first image to the AI as a reference/seed image, and then use this appended prompt):*

```text
A background image suitable to be used as a background for a sidescrolling beat em up game in the vein of Double Dragon and Final Fight. 16-bit pixel art graphics style. No props overlapping with the street. No characters or people in the image. IMPORTANT: Draw all elements vertically stretched (taller and thinner than normal) so that when this square image is later resized/squished into an 800x600 (4:3) aspect ratio, the perspective and proportions will finally look correct. THIS IMAGE MUST BE A SEAMLESS CONTINUATION IMMEDIATELY TO THE RIGHT OF THE PROVIDED REFERENCE IMAGE.
```

### 2. Resizing with FFmpeg

Once the stretched square image is generated and saved locally, use `ffmpeg` to compress the image vertically into its final 4:3 (800x600) resolution.

Ensure your terminal is in the directory containing the original image (or provide absolute paths), and run:

```bash
ffmpeg -y -i <input_image.png> -vf "scale=800:600" <output_image.png>
```
