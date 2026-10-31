# Mesh stage of the CLONE-Z asset pipeline, built and run on the DGX Spark (GB10 Blackwell, sm_121, aarch64).
#
# Holds Z-Image-Turbo (text -> reference picture), BiRefNet (cuts the picture out) and TRELLIS.2 (picture ->
# textured mesh). Everything is downloaded anonymously and runs on the Spark: no account, token or cloud
# service is involved. The pipeline code is mounted at run time rather than copied in, so editing it never
# needs a rebuild; see mesh/README.md.
#
# The CUDA / PyTorch / Triton pins follow github.com/mmanco/spark-trellis2-docker, which documents why:
#  * CUDA 12.9, whose ptxas knows the GB10's sm_121a.
#  * torch 2.7.1+cu128 with Triton replaced by the nightly pytorch-triton build that understands sm_121a; the
#    stock Triton dies with "'sm_121a' is not a recognized processor" on any JIT-compiled kernel.
#  * transformers 4.56.0, because TRELLIS.2 reaches into DINOv3ViTModel internals that later releases renamed.
# Kernels are compiled for sm_120 only. sm_120 code runs on the GB10's sm_121, and leaving out every other
# architecture makes the flash-attn build several times shorter.
FROM nvidia/cuda:12.9.1-cudnn-devel-ubuntu22.04

ARG TRELLIS2_COMMIT=75fbf0183001ed9876c8dbb35de6b68552ee08bd
ARG CUMESH_COMMIT=12289e1062f0603f2f0d0771b02e1395d247f26f
ARG FLEXGEMM_COMMIT=6dd94a859c26ee8246888502eada3dd8ad85532e
ARG NVDIFFREC_COMMIT=b296927cc7fd01c2ac1087c8065c4d7248f72da4
ARG UTILS3D_COMMIT=9a4eb15e4021b67b12c460c7057d642626897ec8
ARG PYTORCH_TRITON_VERSION=3.6.0+git8fedd49b
# Each nvcc job can take several GB; the Spark shares its memory with whatever Ollama has loaded.
ARG MAX_JOBS=3

ENV DEBIAN_FRONTEND=noninteractive \
    CUDA_HOME=/usr/local/cuda-12.9 \
    PATH=/opt/venv/bin:/usr/local/cuda-12.9/bin:$PATH \
    LD_LIBRARY_PATH=/usr/local/cuda-12.9/lib64:$LD_LIBRARY_PATH \
    TORCH_CUDA_ARCH_LIST="12.0+PTX" \
    FLASH_ATTN_CUDA_ARCHS="120" \
    TRITON_PTXAS_PATH=/usr/local/cuda-12.9/bin/ptxas \
    PIP_NO_CACHE_DIR=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1 \
    PYTHONUNBUFFERED=1

# The aarch64 CUDA image has no /usr/local/cuda link, which torch.utils.cpp_extension falls back to. The X11/GL/EGL
# headers are for glcontext (via utils3d's moderngl), which has no aarch64 wheel and compiles from source.
RUN apt-get update && apt-get install -y --no-install-recommends \
        python3.10 python3.10-venv python3.10-dev \
        git wget curl ca-certificates build-essential cmake ninja-build pkg-config \
        libgl1 libglib2.0-0 libegl1 libgles2 libjpeg-dev ffmpeg \
        libx11-dev libxext-dev libgl-dev libegl-dev \
    && rm -rf /var/lib/apt/lists/* \
    && ln -sfn /usr/local/cuda-12.9 /usr/local/cuda \
    && python3.10 -m venv /opt/venv \
    && pip install --upgrade pip setuptools wheel

RUN pip install torch==2.7.1 torchvision==0.22.1 --index-url https://download.pytorch.org/whl/cu128

# TRELLIS.2's "basic" dependencies, minus its web demo (gradio), training-only extras and pillow-simd (which
# does not build on aarch64).
RUN pip install imageio imageio-ffmpeg tqdm easydict opencv-python-headless ninja trimesh scipy pandas lpips \
        zstandard kornia timm pillow transformers==4.56.0 packaging psutil einops \
    && pip install "git+https://github.com/EasternJournalist/utils3d.git@${UTILS3D_COMMIT}"

RUN git clone https://github.com/microsoft/TRELLIS.2.git /opt/TRELLIS.2 \
    && cd /opt/TRELLIS.2 && git checkout "${TRELLIS2_COMMIT}" && git submodule update --init --recursive

RUN mkdir -p /tmp/ext \
    && git clone -b v0.4.0 https://github.com/NVlabs/nvdiffrast.git /tmp/ext/nvdiffrast \
    && MAX_JOBS=${MAX_JOBS} pip install /tmp/ext/nvdiffrast --no-build-isolation

RUN git clone https://github.com/JeffreyXiang/nvdiffrec.git /tmp/ext/nvdiffrec \
    && cd /tmp/ext/nvdiffrec && git checkout "${NVDIFFREC_COMMIT}" \
    && MAX_JOBS=${MAX_JOBS} pip install /tmp/ext/nvdiffrec --no-build-isolation

RUN git clone https://github.com/JeffreyXiang/CuMesh.git /tmp/ext/CuMesh \
    && cd /tmp/ext/CuMesh && git checkout "${CUMESH_COMMIT}" && git submodule update --init --recursive \
    && MAX_JOBS=${MAX_JOBS} pip install /tmp/ext/CuMesh --no-build-isolation

RUN cp -r /opt/TRELLIS.2/o-voxel /tmp/ext/o-voxel \
    && MAX_JOBS=${MAX_JOBS} pip install /tmp/ext/o-voxel --no-build-isolation

# --no-deps: FlexGEMM declares the standard `triton` package, which would undo the Triton swap below.
RUN git clone https://github.com/JeffreyXiang/FlexGEMM.git /tmp/ext/FlexGEMM \
    && cd /tmp/ext/FlexGEMM && git checkout "${FLEXGEMM_COMMIT}" && git submodule update --init --recursive \
    && MAX_JOBS=${MAX_JOBS} pip install /tmp/ext/FlexGEMM --no-build-isolation --no-deps

# TRELLIS.2's sparse attention needs flash-attn (or xformers); 2.8.x is the first release that knows Blackwell.
# FORCE_BUILD stops its setup.py from fetching a prebuilt wheel (there is none for aarch64 anyway).
RUN MAX_JOBS=${MAX_JOBS} NVCC_THREADS=1 FLASH_ATTENTION_FORCE_BUILD=TRUE \
    pip install --no-build-isolation flash-attn==2.8.3

# Z-Image-Turbo for the reference pictures.
RUN pip install diffusers==0.36.0 accelerate

# Last, so nothing installed afterwards can pull the standard triton back in. --no-deps stops the nightly
# wheel from dragging in a nightly torch.
RUN (pip uninstall -y triton pytorch-triton || true) \
    && pip install --pre --no-deps --index-url https://download.pytorch.org/whl/nightly/cu128 \
        "pytorch-triton==${PYTORCH_TRITON_VERSION}" \
    && python -c "import importlib.metadata as m, torch, triton, transformers, diffusers; \
assert torch.__version__.startswith('2.7.'), torch.__version__; \
assert not [d for d in m.distributions() if (d.metadata.get('Name') or '').lower() == 'triton'], 'stock triton is back'; \
print('torch', torch.__version__, '| triton', triton.__version__, '| transformers', transformers.__version__, '| diffusers', diffusers.__version__)" \
    && rm -rf /tmp/ext

ENV PYTHONPATH=/opt/TRELLIS.2 \
    OPENCV_IO_ENABLE_OPENEXR=1 \
    PYTORCH_CUDA_ALLOC_CONF=expandable_segments:True \
    ATTN_BACKEND=flash_attn

WORKDIR /work
CMD ["/bin/bash"]
