```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean             | Error           | StdDev        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |-----------------:|----------------:|--------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **2,159.1 ns** |        **13.26 ns** |       **6.94 ns** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       2,254.2 ns |        30.38 ns |      20.10 ns |  1.04 |    0.01 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |         960.2 ns |         9.25 ns |       6.12 ns |  0.44 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       2,319.6 ns |        24.86 ns |      16.44 ns |  1.07 |    0.01 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       2,346.3 ns |         3.51 ns |       1.84 ns |  1.09 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       2,156.6 ns |       174.16 ns |       9.55 ns |  1.00 |    0.01 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       2,183.9 ns |       134.86 ns |       7.39 ns |  1.01 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |         903.9 ns |        33.77 ns |       1.85 ns |  0.42 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       2,223.2 ns |       105.46 ns |       5.78 ns |  1.03 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       2,326.1 ns |       154.80 ns |       8.48 ns |  1.08 |    0.01 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **47,294,140.7 ns** |   **464,946.33 ns** | **307,533.21 ns** |     **?** |       **?** |      **-** |   **17889 B** |           **?** |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  47,413,059.7 ns | 2,658,224.62 ns | 145,706.26 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **101,521,520.7 ns** |   **283,729.06 ns** | **187,669.20 ns** |     **?** |       **?** |      **-** |  **197101 B** |           **?** |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 101,388,287.1 ns | 4,881,239.32 ns | 267,557.20 ns |     ? |       ? |      - |  197714 B |           ? |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **101,291,801.1 ns** |   **407,753.86 ns** | **269,703.93 ns** |     **?** |       **?** |      **-** |  **430606 B** |           **?** |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 101,323,118.9 ns | 2,414,445.61 ns | 132,343.91 ns |     ? |       ? |      - |  428600 B |           ? |
