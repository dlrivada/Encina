```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.46GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean             | Error           | StdDev          | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |-----------------:|----------------:|----------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **1,216.7 ns** |        **33.40 ns** |        **19.88 ns** |  **1.00** |    **0.02** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       1,239.4 ns |        40.50 ns |        26.79 ns |  1.02 |    0.03 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |         506.7 ns |         6.15 ns |         3.66 ns |  0.42 |    0.01 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       1,267.3 ns |        20.98 ns |        13.88 ns |  1.04 |    0.02 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       1,327.1 ns |        27.18 ns |        17.98 ns |  1.09 |    0.02 | 0.0248 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       1,230.5 ns |        48.08 ns |         2.64 ns |  1.00 |    0.00 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       1,287.3 ns |       579.45 ns |        31.76 ns |  1.05 |    0.02 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |         541.3 ns |        18.83 ns |         1.03 ns |  0.44 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       1,316.0 ns |       142.50 ns |         7.81 ns |  1.07 |    0.01 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       1,367.1 ns |       318.44 ns |        17.45 ns |  1.11 |    0.01 | 0.0248 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **50,339,203.3 ns** | **5,458,502.52 ns** | **3,610,461.45 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  53,175,211.2 ns | 8,677,047.74 ns |   475,618.27 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **101,778,992.0 ns** |   **673,611.14 ns** |   **445,552.06 ns** |     **?** |       **?** |      **-** |  **187958 B** |           **?** |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 101,164,109.1 ns | 3,599,545.79 ns |   197,303.25 ns |     ? |       ? |      - |  190504 B |           ? |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **101,702,259.7 ns** |   **469,100.29 ns** |   **310,280.80 ns** |     **?** |       **?** |      **-** |  **425115 B** |           **?** |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 101,677,441.3 ns | 3,105,278.73 ns |   170,210.81 ns |     ? |       ? |      - |  428451 B |           ? |
