```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean             | Error           | StdDev          | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |-----------------:|----------------:|----------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **1,305.6 ns** |        **11.63 ns** |         **6.92 ns** |  **1.00** |    **0.01** | **0.0038** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       1,331.4 ns |         4.08 ns |         2.13 ns |  1.02 |    0.01 | 0.0038 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |         531.1 ns |         1.70 ns |         1.13 ns |  0.41 |    0.00 | 0.0010 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       1,368.8 ns |         3.21 ns |         2.12 ns |  1.05 |    0.01 | 0.0038 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       1,450.2 ns |         7.70 ns |         4.03 ns |  1.11 |    0.01 | 0.0038 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       1,289.7 ns |        33.95 ns |         1.86 ns |  1.00 |    0.00 | 0.0038 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       1,328.6 ns |        30.26 ns |         1.66 ns |  1.03 |    0.00 | 0.0038 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |         532.1 ns |        24.48 ns |         1.34 ns |  0.41 |    0.00 | 0.0010 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       1,362.3 ns |        32.05 ns |         1.76 ns |  1.06 |    0.00 | 0.0038 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       1,439.4 ns |        82.65 ns |         4.53 ns |  1.12 |    0.00 | 0.0038 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **46,936,454.1 ns** | **1,598,035.67 ns** | **1,057,001.65 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  48,434,161.5 ns | 6,305,773.18 ns |   345,640.71 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **101,569,563.9 ns** |   **325,766.55 ns** |   **193,858.45 ns** |     **?** |       **?** |      **-** |  **185155 B** |           **?** |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 101,462,037.7 ns | 3,926,890.18 ns |   215,246.10 ns |     ? |       ? |      - |  189382 B |           ? |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **101,539,542.8 ns** |   **540,942.03 ns** |   **357,799.66 ns** |     **?** |       **?** |      **-** |  **401998 B** |           **?** |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 101,780,331.9 ns | 3,582,069.08 ns |   196,345.29 ns |     ? |       ? |      - |  405491 B |           ? |
