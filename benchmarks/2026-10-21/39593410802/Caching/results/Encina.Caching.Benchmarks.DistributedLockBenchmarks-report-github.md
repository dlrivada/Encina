```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.46GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean           | Error         | StdDev      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |---------------:|--------------:|------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **2.440 μs** |     **0.0135 μs** |   **0.0071 μs** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       2.530 μs |     0.0077 μs |   0.0046 μs |  1.04 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |       1.157 μs |     0.0049 μs |   0.0029 μs |  0.47 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       2.548 μs |     0.0115 μs |   0.0068 μs |  1.04 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       2.614 μs |     0.0038 μs |   0.0020 μs |  1.07 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |                  |                |               |             |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       2.481 μs |     0.1315 μs |   0.0072 μs |  1.00 |    0.00 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       2.508 μs |     0.2483 μs |   0.0136 μs |  1.01 |    0.01 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |       1.157 μs |     0.2946 μs |   0.0161 μs |  0.47 |    0.01 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       2.542 μs |     0.1669 μs |   0.0091 μs |  1.02 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       2.648 μs |     0.1014 μs |   0.0056 μs |  1.07 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |                  |                |               |             |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **47,152.959 μs** | **1,082.8876 μs** | **716.2631 μs** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |                  |                |               |             |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  47,619.314 μs | 5,130.7907 μs | 281.2360 μs |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |                  |                |               |             |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **101,745.713 μs** |   **490.2362 μs** | **324.2609 μs** |     **?** |       **?** |      **-** |  **194768 B** |           **?** |
|                          |            |                |             |                  |                |               |             |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 101,619.574 μs | 4,203.5248 μs | 230.4094 μs |     ? |       ? |      - |  195142 B |           ? |
|                          |            |                |             |                  |                |               |             |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **101,861.529 μs** |   **456.1465 μs** | **301.7127 μs** |     **?** |       **?** |      **-** |  **432434 B** |           **?** |
|                          |            |                |             |                  |                |               |             |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 101,360.301 μs | 2,417.5970 μs | 132.5166 μs |     ? |       ? |      - |  431861 B |           ? |
