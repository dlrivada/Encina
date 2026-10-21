```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean             | Error           | StdDev        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |-----------------:|----------------:|--------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **1,425.2 ns** |         **2.27 ns** |       **1.50 ns** |  **1.00** |    **0.00** | **0.0153** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       1,449.6 ns |         3.33 ns |       2.20 ns |  1.02 |    0.00 | 0.0153 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |         526.0 ns |         1.26 ns |       0.75 ns |  0.37 |    0.00 | 0.0038 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       1,486.3 ns |         4.73 ns |       3.13 ns |  1.04 |    0.00 | 0.0153 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       1,613.1 ns |         3.40 ns |       1.78 ns |  1.13 |    0.00 | 0.0172 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       1,405.3 ns |        57.13 ns |       3.13 ns |  1.00 |    0.00 | 0.0153 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       1,434.2 ns |        37.28 ns |       2.04 ns |  1.02 |    0.00 | 0.0153 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |         526.8 ns |         7.80 ns |       0.43 ns |  0.37 |    0.00 | 0.0038 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       1,470.5 ns |        28.83 ns |       1.58 ns |  1.05 |    0.00 | 0.0153 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       1,529.6 ns |        20.09 ns |       1.10 ns |  1.09 |    0.00 | 0.0172 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **46,834,517.3 ns** |   **807,342.00 ns** | **534,006.75 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  47,251,882.3 ns | 4,963,792.47 ns | 272,082.22 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **101,751,141.3 ns** |   **445,826.87 ns** | **294,886.87 ns** |     **?** |       **?** |      **-** |  **191418 B** |           **?** |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 101,570,908.3 ns | 4,325,874.30 ns | 237,115.77 ns |     ? |       ? |      - |  189539 B |           ? |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **101,620,539.0 ns** |   **218,495.66 ns** | **130,023.26 ns** |     **?** |       **?** |      **-** |  **425224 B** |           **?** |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 101,480,567.2 ns | 1,452,642.34 ns |  79,624.23 ns |     ? |       ? |      - |  418378 B |           ? |
