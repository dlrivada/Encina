```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean             | Error            | StdDev        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |-----------------:|-----------------:|--------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **1,818.0 ns** |         **47.62 ns** |      **31.50 ns** |  **1.00** |    **0.02** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       1,904.6 ns |         61.45 ns |      40.65 ns |  1.05 |    0.03 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |         851.0 ns |         19.23 ns |      12.72 ns |  0.47 |    0.01 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       1,872.4 ns |        118.42 ns |      70.47 ns |  1.03 |    0.04 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       1,959.7 ns |         57.64 ns |      38.13 ns |  1.08 |    0.03 | 0.0248 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       1,785.6 ns |        880.67 ns |      48.27 ns |  1.00 |    0.03 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       1,863.4 ns |      1,719.97 ns |      94.28 ns |  1.04 |    0.05 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |         834.7 ns |        233.70 ns |      12.81 ns |  0.47 |    0.01 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       1,870.2 ns |        565.56 ns |      31.00 ns |  1.05 |    0.03 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       1,937.6 ns |      1,175.03 ns |      64.41 ns |  1.09 |    0.04 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **47,192,329.5 ns** |    **450,202.82 ns** | **297,781.29 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  47,182,189.5 ns |  5,694,951.50 ns | 312,159.51 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **103,207,447.5 ns** |    **939,467.10 ns** | **621,399.32 ns** |     **?** |       **?** |      **-** |  **187675 B** |           **?** |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 103,026,329.7 ns | 11,234,573.29 ns | 615,804.87 ns |     ? |       ? |      - |  191904 B |           ? |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **103,055,597.9 ns** |  **1,017,683.74 ns** | **673,134.78 ns** |     **?** |       **?** |      **-** |  **416677 B** |           **?** |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 102,463,130.1 ns |  3,388,271.55 ns | 185,722.60 ns |     ? |       ? |      - |  411250 B |           ? |
