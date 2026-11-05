```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.42GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean             | Error           | StdDev        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |-----------------:|----------------:|--------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **1,386.0 ns** |         **5.64 ns** |       **3.36 ns** |  **1.00** |    **0.00** | **0.0153** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       1,373.2 ns |         1.91 ns |       1.14 ns |  0.99 |    0.00 | 0.0153 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |         538.7 ns |         1.15 ns |       0.76 ns |  0.39 |    0.00 | 0.0038 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       1,437.1 ns |         4.40 ns |       2.91 ns |  1.04 |    0.00 | 0.0153 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       1,530.0 ns |         4.49 ns |       2.67 ns |  1.10 |    0.00 | 0.0172 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       1,357.4 ns |       105.39 ns |       5.78 ns |  1.00 |    0.01 | 0.0153 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       1,411.4 ns |        83.47 ns |       4.58 ns |  1.04 |    0.00 | 0.0153 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |         544.9 ns |        27.15 ns |       1.49 ns |  0.40 |    0.00 | 0.0038 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       1,465.5 ns |        67.68 ns |       3.71 ns |  1.08 |    0.00 | 0.0153 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       1,546.2 ns |        36.14 ns |       1.98 ns |  1.14 |    0.00 | 0.0172 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **47,621,994.6 ns** |   **852,214.38 ns** | **563,687.05 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  47,848,539.5 ns | 9,585,088.88 ns | 525,391.07 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **101,659,316.6 ns** |   **530,675.43 ns** | **351,008.94 ns** |     **?** |       **?** |      **-** |  **192821 B** |           **?** |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 101,403,130.4 ns | 2,149,078.14 ns | 117,798.23 ns |     ? |       ? |      - |  188022 B |           ? |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **101,433,967.4 ns** |   **330,889.97 ns** | **196,907.31 ns** |     **?** |       **?** |      **-** |  **413341 B** |           **?** |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 101,405,212.3 ns |   794,534.60 ns |  43,551.12 ns |     ? |       ? |      - |  420461 B |           ? |
