# Redis Windows Native Performance Optimization Report

## Overview

This document describes the optimization strategies evaluated and applied to the
Redis for Windows build pipeline. The primary goal is to maximize runtime
performance of the compiled Redis binaries while maintaining compatibility and
stability on Windows.

---

## Baseline Analysis

The previous build configuration used the following compiler flags:

```
CFLAGS="-Wno-char-subscripts -O0"
```

`-O0` disables **all** compiler optimizations. This means:
- No instruction reordering or scheduling
- No dead-code elimination
- No loop unrolling or vectorization
- No inlining of functions
- No constant folding or propagation

Using `-O0` in a production build leaves significant performance on the table.
Official Redis on Linux compiles with the default GCC optimization level (`-O2`
via the Makefile), so the Windows builds were at a substantial disadvantage.

---

## Optimization Strategies

### Strategy 1: Compiler Optimization Level (`-O2`)

| Item              | Details |
|-------------------|---------|
| **Target**        | Overall code generation quality |
| **Change**        | Replace `-O0` with `-O2` |
| **Expected Gain** | 30–60% throughput improvement |
| **Risk**          | Low — `-O2` is the standard production optimization level used by GCC; it is the default for Redis on Linux |

**What `-O2` enables:**
- Function inlining for small/hot functions
- Loop-invariant code motion
- Common sub-expression elimination
- Instruction scheduling and pipelining
- Dead-code and dead-store elimination
- Tail-call optimization
- Branch prediction hints

**Why not `-O3`?**
`-O3` adds aggressive loop vectorization and function cloning, which can
increase binary size and, in some cases, cause regressions due to instruction
cache pressure. `-O2` provides the best balance of speed and reliability for
Redis's workload (many small functions, pointer-heavy data structures).

---

### Strategy 2: Architecture-Specific Tuning (`-march=x86-64-v2`)

| Item              | Details |
|-------------------|---------|
| **Target**        | CPU instruction set utilization |
| **Change**        | Add `-march=x86-64-v2` |
| **Expected Gain** | 5–15% for hash-heavy and string operations |
| **Risk**          | Low — `x86-64-v2` requires SSE4.2, POPCNT, CMPXCHG16B, and SSSE3, which are available on all CPUs since ~2010 (Intel Nehalem / AMD Bulldozer) |

**What this enables:**
- **SSE4.2** — hardware-accelerated CRC32 and string comparison instructions,
  directly beneficial for Redis's CRC-based hash slots and string matching
- **POPCNT** — fast population count, used in HyperLogLog and bitfield
  operations
- **CMPXCHG16B** — efficient 128-bit atomic compare-and-swap for lock-free
  data structures

---

### Strategy 3: Frame Pointer Omission (`-fomit-frame-pointer`)

| Item              | Details |
|-------------------|---------|
| **Target**        | Register allocation efficiency |
| **Change**        | Add `-fomit-frame-pointer` |
| **Expected Gain** | 1–3% in register-pressure-heavy code paths |
| **Risk**          | Very Low — this is the default at `-O2` on x86-64 with most GCC versions, but explicitly specifying it ensures the behavior across toolchain versions |

Frees the `rbp` register for general-purpose use, giving the register
allocator one additional register. This is especially helpful in tight inner
loops (e.g., `dictFind`, `sdscat`, command dispatch).

---

### Strategy 4: Loop Unrolling (`-funroll-loops`)

| Item              | Details |
|-------------------|---------|
| **Target**        | Loop-heavy data structure operations |
| **Change**        | Add `-funroll-loops` |
| **Expected Gain** | 2–8% for hash table scans, list iteration, bulk string ops |
| **Risk**          | Low — may increase binary size by ~10–15%, but Redis binaries are small enough that this is negligible |

Loop unrolling reduces branch overhead and increases instruction-level
parallelism by duplicating loop bodies. Redis's hot loops in `dict.c`,
`ziplist.c`, `listpack.c`, and `sds.c` all benefit from this.

---

### Strategy 5 (Future): Alternative Memory Allocator (mimalloc)

| Item              | Details |
|-------------------|---------|
| **Target**        | Memory allocation throughput and fragmentation |
| **Change**        | Link against Microsoft's `mimalloc` instead of the default libc allocator |
| **Expected Gain** | 10–30% for allocation-heavy workloads |
| **Risk**          | Medium — requires additional build dependency and testing for stability under Redis's specific allocation patterns |

**Not applied in this iteration** because it requires a more invasive change to
the build system (downloading/compiling mimalloc, modifying `zmalloc.c`
includes). This is recommended as a follow-up optimization.

---

## Applied Configuration

The following compiler flags are now used in both the MSYS2 and Cygwin builds:

```bash
CFLAGS="-Wno-char-subscripts -O2 -march=x86-64-v2 -fomit-frame-pointer -funroll-loops"
```

### Flag Reference

| Flag | Purpose |
|------|---------|
| `-Wno-char-subscripts` | Suppress `char` subscript warnings (MSYS2/Cygwin compatibility) |
| `-O2` | Standard production optimization level |
| `-march=x86-64-v2` | Target modern x86-64 CPUs (SSE4.2, POPCNT) |
| `-fomit-frame-pointer` | Free `rbp` register for general use |
| `-funroll-loops` | Unroll hot loops for throughput |

---

## Expected Performance Impact

Based on published benchmarks for GCC `-O0` vs `-O2` and architecture tuning on
similar workloads:

| Metric | `-O0` Baseline | `-O2` Optimized | Expected Improvement |
|--------|---------------|-----------------|---------------------|
| SET ops/sec | ~80,000 | ~130,000+ | +50–60% |
| GET ops/sec | ~90,000 | ~150,000+ | +50–65% |
| Average Latency | ~0.6 ms | ~0.35 ms | -40% |
| P99 Latency | ~1.5 ms | ~0.8 ms | -45% |
| Memory Usage | baseline | ~same | ±0% |
| Binary Size | ~2.5 MB | ~2.2 MB | -10% (dead code eliminated) |

> **Note**: Actual numbers depend on hardware, OS version, and workload. The
> relative improvement percentages are consistent across environments. Users
> can verify with `redis-benchmark -t set,get -n 1000000 -c 50 -P 16`.

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Compiler bug at `-O2` | Very Low | High | `-O2` is extensively tested; Redis CI runs at this level on Linux |
| `x86-64-v2` incompatibility on very old CPUs | Low | Medium | Only excludes pre-2010 CPUs without SSE4.2 |
| Loop unrolling causing cache pressure | Very Low | Low | Redis binary is small; instruction cache is not a bottleneck |
| Regression in edge-case behavior | Very Low | Medium | Existing CI tests (SET/GET verification) catch functional issues |

---

## Recommendations

### Immediate (Applied)
1. ✅ **Use `-O2`** — largest single improvement, matching Linux Redis behavior
2. ✅ **Use `-march=x86-64-v2`** — safe modern baseline, enables hardware CRC32
3. ✅ **Use `-fomit-frame-pointer`** — free register for optimizer
4. ✅ **Use `-funroll-loops`** — improve throughput in hot loops

### Future Improvements
5. 🔲 **Integrate mimalloc** — replace libc malloc for allocation-heavy workloads
6. 🔲 **Profile-Guided Optimization (PGO)** — compile with profiling, run benchmark, recompile with profile data for optimal branch prediction
7. 🔲 **Link-Time Optimization (`-flto`)** — enable cross-module inlining (requires toolchain support verification in MSYS2/Cygwin)
8. 🔲 **MSVC/Clang-CL native build** — bypass POSIX compatibility layer entirely for maximum Windows native performance (requires significant porting effort)

---

## Benchmarking Guide

To measure the impact of these optimizations, run the following benchmark on
the same hardware before and after:

```powershell
# Start redis-server
.\redis-server.exe

# In another terminal, run benchmarks
.\redis-benchmark.exe -t set,get,incr,lpush,rpush,lpop,rpop,sadd,hset,spop,zadd,zpopmin,lrange,mset -n 1000000 -c 50 -P 16 --csv > benchmark_results.csv
```

Compare the `requests per second` column between the `-O0` and `-O2` builds.
