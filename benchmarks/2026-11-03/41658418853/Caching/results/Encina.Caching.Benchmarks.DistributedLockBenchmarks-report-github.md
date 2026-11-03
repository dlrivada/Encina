```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.03GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean             | Error           | StdDev        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |-----------------:|----------------:|--------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **1,316.0 ns** |         **3.81 ns** |       **2.27 ns** |  **1.00** |    **0.00** | **0.0038** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       1,335.7 ns |         3.22 ns |       2.13 ns |  1.01 |    0.00 | 0.0038 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |         522.4 ns |         3.04 ns |       1.81 ns |  0.40 |    0.00 | 0.0010 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       1,377.6 ns |         2.21 ns |       1.32 ns |  1.05 |    0.00 | 0.0038 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       1,444.0 ns |         5.26 ns |       3.48 ns |  1.10 |    0.00 | 0.0038 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       1,298.4 ns |        31.47 ns |       1.72 ns |  1.00 |    0.00 | 0.0038 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       1,342.6 ns |        48.43 ns |       2.65 ns |  1.03 |    0.00 | 0.0038 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |         526.6 ns |        18.05 ns |       0.99 ns |  0.41 |    0.00 | 0.0010 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       1,372.6 ns |        57.84 ns |       3.17 ns |  1.06 |    0.00 | 0.0038 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       1,446.9 ns |       120.83 ns |       6.62 ns |  1.11 |    0.00 | 0.0038 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **47,533,623.6 ns** |   **792,701.39 ns** | **524,322.89 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  47,935,636.5 ns | 6,110,638.72 ns | 334,944.73 ns |     ? |       ? |      - |   17646 B |           ? |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **101,518,590.8 ns** |   **299,743.75 ns** | **198,261.93 ns** |     **?** |       **?** |      **-** |  **189195 B** |           **?** |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 101,646,302.2 ns | 5,337,610.55 ns | 292,572.45 ns |     ? |       ? |      - |  192795 B |           ? |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **101,540,018.1 ns** |   **342,405.05 ns** | **226,479.74 ns** |     **?** |       **?** |      **-** |  **417803 B** |           **?** |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 101,478,240.9 ns | 2,897,466.30 ns | 158,819.91 ns |     ? |       ? |      - |  411667 B |           ? |
