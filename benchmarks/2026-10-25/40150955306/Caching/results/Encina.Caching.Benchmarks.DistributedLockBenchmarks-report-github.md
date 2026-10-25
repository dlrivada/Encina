```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | Mean             | Error         | StdDev        | Median           | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |----------------- |-----------------:|--------------:|--------------:|-----------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                |       **1,633.5 ns** |       **4.18 ns** |       **2.49 ns** |       **1,633.2 ns** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | 3           | ?                |       1,673.5 ns |       9.40 ns |       6.22 ns |       1,675.5 ns |  1.02 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | 3           | ?                |         750.8 ns |       1.22 ns |       0.64 ns |         750.7 ns |  0.46 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | 3           | ?                |       1,684.7 ns |       4.85 ns |       2.54 ns |       1,684.4 ns |  1.03 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | 3           | ?                |       1,750.1 ns |       1.04 ns |       0.62 ns |       1,750.1 ns |  1.07 |    0.00 | 0.0248 |     432 B |        1.10 |
|                          |            |                |             |             |                  |                  |               |               |                  |       |         |        |           |             |
| AcquireAndReleaseLock    | MediumRun  | 15             | 2           | 10          | ?                |       1,659.3 ns |      26.38 ns |      36.98 ns |       1,631.8 ns |  1.00 |    0.03 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | MediumRun  | 15             | 2           | 10          | ?                |       1,687.8 ns |       4.62 ns |       6.47 ns |       1,689.4 ns |  1.02 |    0.02 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | MediumRun  | 15             | 2           | 10          | ?                |         761.0 ns |       1.24 ns |       1.70 ns |         761.6 ns |  0.46 |    0.01 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | MediumRun  | 15             | 2           | 10          | ?                |       1,709.5 ns |      15.22 ns |      20.83 ns |       1,710.6 ns |  1.03 |    0.03 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | MediumRun  | 15             | 2           | 10          | ?                |       1,747.9 ns |       4.85 ns |       7.26 ns |       1,746.1 ns |  1.05 |    0.02 | 0.0248 |     432 B |        1.10 |
|                          |            |                |             |             |                  |                  |               |               |                  |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               |  **47,462,242.0 ns** | **433,550.47 ns** | **286,766.79 ns** |  **47,477,268.4 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |             |                  |                  |               |               |                  |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 10               |  47,366,518.5 ns | 212,451.85 ns | 311,409.19 ns |  47,325,345.6 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |             |                  |                  |               |               |                  |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **101,143,280.4 ns** | **281,107.96 ns** | **167,282.84 ns** | **101,184,786.6 ns** |     **?** |       **?** |      **-** |  **196542 B** |           **?** |
|                          |            |                |             |             |                  |                  |               |               |                  |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 50               | 101,210,064.9 ns | 182,530.62 ns | 273,203.26 ns | 101,205,813.2 ns |     ? |       ? |      - |  192970 B |           ? |
|                          |            |                |             |             |                  |                  |               |               |                  |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **100**              | **101,438,545.8 ns** | **696,941.63 ns** | **460,983.73 ns** | **101,510,252.5 ns** |     **?** |       **?** |      **-** |  **435101 B** |           **?** |
|                          |            |                |             |             |                  |                  |               |               |                  |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 100              | 101,404,901.1 ns | 173,021.51 ns | 258,970.47 ns | 101,424,528.0 ns |     ? |       ? |      - |  428952 B |           ? |
