```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean             | Error            | StdDev          | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |-----------------:|-----------------:|----------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **1,225.8 ns** |         **20.22 ns** |        **12.03 ns** |  **1.00** |    **0.01** | **0.0038** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       1,274.0 ns |         21.12 ns |        13.97 ns |  1.04 |    0.01 | 0.0038 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |         497.9 ns |          5.42 ns |         3.59 ns |  0.41 |    0.00 | 0.0010 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       1,305.4 ns |         22.04 ns |        14.58 ns |  1.07 |    0.02 | 0.0038 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       1,386.3 ns |         23.19 ns |        13.80 ns |  1.13 |    0.02 | 0.0038 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       1,226.2 ns |        183.83 ns |        10.08 ns |  1.00 |    0.01 | 0.0038 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       1,264.9 ns |         47.58 ns |         2.61 ns |  1.03 |    0.01 | 0.0038 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |         505.8 ns |        112.74 ns |         6.18 ns |  0.41 |    0.01 | 0.0010 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       1,303.3 ns |        367.55 ns |        20.15 ns |  1.06 |    0.02 | 0.0038 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       1,358.8 ns |        125.93 ns |         6.90 ns |  1.11 |    0.01 | 0.0038 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **50,118,887.3 ns** |  **1,418,242.03 ns** |   **938,079.29 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  52,561,220.6 ns | 42,360,447.88 ns | 2,321,919.10 ns |     ? |       ? |      - |   17744 B |           ? |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **101,738,019.0 ns** |    **429,186.63 ns** |   **283,880.38 ns** |     **?** |       **?** |      **-** |  **192822 B** |           **?** |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 101,952,918.2 ns |    918,846.31 ns |    50,365.07 ns |     ? |       ? |      - |  185466 B |           ? |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **101,852,527.6 ns** |    **699,284.54 ns** |   **462,533.42 ns** |     **?** |       **?** |      **-** |  **428688 B** |           **?** |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 101,595,995.1 ns |  1,817,010.99 ns |    99,596.50 ns |     ? |       ? |      - |  408862 B |           ? |
