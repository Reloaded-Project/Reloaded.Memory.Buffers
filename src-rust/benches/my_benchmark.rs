use criterion::{criterion_group, criterion_main, Criterion};
use reloaded_memory_buffers::{
    buffers::Buffers, buffers_api::BuffersApi, structs::params::BufferSearchSettings,
};

#[cfg(not(target_os = "windows"))]
use pprof::criterion::{Output, PProfProfiler};

fn get_buffer() {
    let settings = BufferSearchSettings {
        min_address: 0_usize,
        max_address: i32::MAX as usize,
        size: 4096,
    };

    // Automatically dropped.
    let _item = Buffers::get_buffer(&settings).unwrap();
}

fn criterion_benchmark(c: &mut Criterion) {
    c.bench_function("get_buffer", |b| b.iter(get_buffer));
}

#[cfg(not(target_os = "windows"))]
criterion_group! {
    name = benches;
    config = Criterion::default().with_profiler(PProfProfiler::new(1000, Output::Flamegraph(None)));
    targets = criterion_benchmark
}

#[cfg(target_os = "windows")]
criterion_group! {
    name = benches;
    config = Criterion::default();
    targets = criterion_benchmark
}

criterion_main!(benches);
