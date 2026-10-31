```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 0.80GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean             | Error            | StdDev          | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |-----------------:|-----------------:|----------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **1,439.7 ns** |         **21.17 ns** |        **14.00 ns** |  **1.00** |    **0.01** | **0.0153** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       1,503.9 ns |         34.18 ns |        22.61 ns |  1.04 |    0.02 | 0.0153 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |         541.8 ns |         17.28 ns |        11.43 ns |  0.38 |    0.01 | 0.0038 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       1,542.2 ns |         37.48 ns |        22.30 ns |  1.07 |    0.02 | 0.0153 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       1,604.4 ns |         35.98 ns |        21.41 ns |  1.11 |    0.02 | 0.0172 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       1,390.4 ns |        277.96 ns |        15.24 ns |  1.00 |    0.01 | 0.0153 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       1,442.9 ns |        203.35 ns |        11.15 ns |  1.04 |    0.01 | 0.0153 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |         543.3 ns |        132.65 ns |         7.27 ns |  0.39 |    0.01 | 0.0038 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       1,502.5 ns |        262.23 ns |        14.37 ns |  1.08 |    0.01 | 0.0153 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       1,551.5 ns |        113.26 ns |         6.21 ns |  1.12 |    0.01 | 0.0172 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **54,290,097.8 ns** |  **1,492,204.89 ns** |   **987,001.14 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  53,813,724.5 ns | 18,194,669.43 ns |   997,311.23 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **102,171,413.8 ns** |    **713,908.38 ns** |   **373,387.89 ns** |     **?** |       **?** |      **-** |  **174949 B** |           **?** |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 102,059,955.1 ns |  7,139,342.69 ns |   391,331.47 ns |     ? |       ? |      - |  179882 B |           ? |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **102,184,021.1 ns** |    **524,483.03 ns** |   **274,314.76 ns** |     **?** |       **?** |      **-** |  **383709 B** |           **?** |
|                          |            |                |             |                  |                  |                  |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 102,206,661.7 ns | 21,330,821.52 ns | 1,169,214.31 ns |     ? |       ? |      - |  386162 B |           ? |
