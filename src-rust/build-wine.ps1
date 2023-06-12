# Make sure you have Docker/Podman first
rustup target add x86_64-pc-windows-gnu
cross build --target x86_64-pc-windows-gnu
cross test --target x86_64-pc-windows-gnu