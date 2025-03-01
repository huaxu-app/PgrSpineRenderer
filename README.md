# PGR Spine Renderer

PgrSpineRenderer allows you to take the index files generated by [pgr-assets](https://github.com/huaxu-app/pgr-assets) and render that prefab into an actual videos.
This renderer will account for the different way the files could be loaded, ordering of the various spine layers that build up a single PGR prefab, and implements
some Unity-specific concepts (such as the BoneFollower) to get near-parity to the animations used in-game.
Furthermore, in the same spirit as the rest of Huaxu's asset tools, this renderer is designed to be able to skip work that has not changed,
allowing me to incrementally update the game's assets without having to re-render everything every update.

While the animations rendered might not be perfect right now, they are in the realm of "good enough".

## Usage

```bash
# Simple usage: render one video
PgrSpineRenderer "path/to/index.json"
```

```bash
PgrSpineRenderer
Description:
  Renders the animations of the specified index files to video.
  Animations will be written to the `render` directory in the same directory as the index file,
  with each animation being a separate video.
  
  A symbolic link to the default animation will be created as `_default.{ext}`,
  allowing for easy access to the default animation without knowing the name.
  
  When multiple index files are specified, they will be rendered in parallel,
  depending on the number of threads specified.
  
  By default the renderer will render to VP9 (webm) at 30fps.

Usage:
  PgrSpineRenderer <indexes>... [options]

Arguments:
  <indexes>  The index files for the spines to render

Options:
  --fps <fps>                              The frames per second of the output video [default: 30]
  -c, --codec <H264|H264NV|VP9>            The codec to use for the output videos. Defaults to 'vp9' 
                                           [default: VP9]
  --encode-threads, --et <encode-threads>  The number of threads to use for encoding. Might be capped by 
                                           hardware limits (nvenc) [default: 1]
  --render-threads, --rt <render-threads>  The number of threads to use for rendering [default: 1]
  -f, --force                              Force rendering even if the index file has not changed
  --version                                Show version information
  -?, -h, --help                           Show help and usage information
```

