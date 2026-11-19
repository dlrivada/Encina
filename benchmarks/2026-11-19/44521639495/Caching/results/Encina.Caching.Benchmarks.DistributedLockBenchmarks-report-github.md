```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.09GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean             | Error            | StdDev          | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |-----------------:|-----------------:|----------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **1,350.1 ns** |         **13.00 ns** |         **7.74 ns** |  **1.00** |    **0.01** | **0.0038** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       1,378.2 ns |          8.13 ns |         5.38 ns |  1.02 |    0.01 | 0.0038 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |         545.5 ns |          4.56 ns |         2.71 ns |  0.40 |    0.00 | 0.0010 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       1,446.2 ns |         59.40 ns |        39.29 ns |  1.07 |    0.03 | 0.0038 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       1,486.8 ns |          8.59 ns |         5.11 ns |  1.10 |    0.01 | 0.0038 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       1,358.0 ns |        295.75 ns |        16.21 ns |  1.00 |    0.01 | 0.0038 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       1,377.8 ns |         82.21 ns |         4.51 ns |  1.01 |    0.01 | 0.0038 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |         538.3 ns |         75.04 ns |         4.11 ns |  0.40 |    0.00 | 0.0010 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       1,407.5 ns |         64.78 ns |         3.55 ns |  1.04 |    0.01 | 0.0038 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       1,479.0 ns |         81.36 ns |         4.46 ns |  1.09 |    0.01 | 0.0038 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **50,070,844.9 ns** |  **1,832,456.59 ns** | **1,212,056.58 ns** |     **?** |       **?** |      **-** |   **17646 B** |           **?** |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  46,387,654.2 ns | 21,293,134.01 ns | 1,167,148.53 ns |     ? |       ? |      - |   17904 B |           ? |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **101,754,670.3 ns** |    **430,884.26 ns** |   **256,412.31 ns** |     **?** |       **?** |      **-** |  **189594 B** |           **?** |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 101,936,707.3 ns |  4,083,401.77 ns |   223,825.03 ns |     ? |       ? |      - |  182078 B |           ? |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **101,996,316.4 ns** |    **630,666.97 ns** |   **417,147.15 ns** |     **?** |       **?** |      **-** |  **409131 B** |           **?** |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 101,586,353.9 ns |    959,024.37 ns |    52,567.36 ns |     ? |       ? |      - |  418530 B |           ? |
