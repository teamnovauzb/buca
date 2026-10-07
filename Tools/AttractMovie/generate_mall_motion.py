"""Local LTX-2.3 motion generation with sequential text-encoder loading.

Designed for the 16 GB M2 used for this project. Gemma and the 6 GB
connector are never resident together. This wrapper leaves the game alone.
Run with the isolated /private/tmp/buca-local-video/venv Python.
"""

from __future__ import annotations

import gc
import hashlib
import os
from pathlib import Path

os.environ.setdefault("LTX2_VAE_DECODE_BUDGET_GB", "3")
os.environ.setdefault("HF_HUB_OFFLINE", "1")
os.environ.setdefault("TOKENIZERS_PARALLELISM", "false")

import mlx.core as mx

from ltx_core_mlx.text_encoders.gemma.encoders.base_encoder import GemmaLanguageModel
from ltx_core_mlx.text_encoders.gemma.feature_extractor import GemmaFeaturesExtractorV2
from ltx_core_mlx.utils.memory import aggressive_cleanup
from ltx_pipelines_mlx.utils import blocks

CACHE = Path(__file__).resolve().parents[2] / "output/buca-mall-film/embeddings"


def sequential_encode(self, prompt: str):
    CACHE.mkdir(parents=True, exist_ok=True)
    key = hashlib.sha256((str(self.model_dir) + "\n" + prompt).encode()).hexdigest()
    target = CACHE / f"{key}.safetensors"
    if target.exists():
        saved = mx.load(str(target))
        print("Using cached prompt embeddings.", flush=True)
        return saved["video"], saved["audio"]

    print("Encoding prompt with Gemma alone (memory-safe phase 1).", flush=True)
    encoder = GemmaLanguageModel()
    encoder.load(self.gemma_model_id)
    states, mask = encoder.encode_all_layers(prompt, max_length=1024)
    mx.eval(*states, mask)
    del encoder
    gc.collect()
    aggressive_cleanup()

    print("Loading connector after releasing Gemma (memory-safe phase 2).", flush=True)
    config = blocks.LTXModelConfig.from_checkpoint_dir(self.model_dir)
    extractor = GemmaFeaturesExtractorV2(double_precision_rope=config.double_precision_rope)
    weights = blocks.load_split_safetensors(self.model_dir / "connector.safetensors", prefix="connector.")
    extractor.connector.load_weights(list(weights.items()))
    del weights
    video, audio = extractor(states, attention_mask=mask)
    mx.eval(video, audio)
    mx.save_safetensors(str(target), {"video": video, "audio": audio})
    del states, mask, extractor
    gc.collect()
    aggressive_cleanup()
    print("Prompt ready; released text models before motion generation.", flush=True)
    return video, audio


def main():
    # Do not reserve all unified RAM: leave space for macOS and existing apps.
    mx.set_cache_limit(0)
    mx.set_memory_limit(10 * 1024**3)
    blocks.PromptEncoder.load = lambda self: None
    blocks.PromptEncoder.encode = sequential_encode
    from ltx_pipelines_mlx.cli import main as cli_main
    cli_main()


if __name__ == "__main__":
    main()
