```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean             | Error            | StdDev        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |-----------------:|-----------------:|--------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **1,303.9 ns** |         **18.34 ns** |       **9.59 ns** |  **1.00** |    **0.01** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       1,318.1 ns |         20.63 ns |      12.27 ns |  1.01 |    0.01 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |         538.2 ns |          5.35 ns |       2.80 ns |  0.41 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       1,372.9 ns |         30.06 ns |      19.88 ns |  1.05 |    0.02 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       1,376.6 ns |         25.05 ns |      16.57 ns |  1.06 |    0.01 | 0.0248 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       1,304.7 ns |        578.84 ns |      31.73 ns |  1.00 |    0.03 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       1,315.6 ns |        131.45 ns |       7.21 ns |  1.01 |    0.02 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |         532.8 ns |         70.27 ns |       3.85 ns |  0.41 |    0.01 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       1,403.4 ns |        132.20 ns |       7.25 ns |  1.08 |    0.02 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       1,402.2 ns |        170.88 ns |       9.37 ns |  1.08 |    0.02 | 0.0248 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **49,217,207.8 ns** |  **1,277,377.95 ns** | **844,906.42 ns** |     **?** |       **?** |      **-** |   **17646 B** |           **?** |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  48,414,206.6 ns | 16,588,954.80 ns | 909,296.59 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **101,319,746.3 ns** |    **439,372.95 ns** | **290,618.00 ns** |     **?** |       **?** |      **-** |  **194117 B** |           **?** |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 101,486,921.2 ns |    808,718.39 ns |  44,328.58 ns |     ? |       ? |      - |  192970 B |           ? |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **101,769,763.9 ns** |    **419,738.34 ns** | **277,630.92 ns** |     **?** |       **?** |      **-** |  **422990 B** |           **?** |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 101,455,158.7 ns |  1,867,489.76 ns | 102,363.42 ns |     ? |       ? |      - |  435088 B |           ? |
