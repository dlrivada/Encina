```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | Mean             | Error         | StdDev        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |----------------- |-----------------:|--------------:|--------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                |       **1,630.3 ns** |       **6.66 ns** |       **3.96 ns** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | 3           | ?                |       1,703.9 ns |      23.86 ns |      15.78 ns |  1.05 |    0.01 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | 3           | ?                |         756.9 ns |       7.47 ns |       4.44 ns |  0.46 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | 3           | ?                |       1,680.7 ns |      15.70 ns |      10.39 ns |  1.03 |    0.01 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | 3           | ?                |       1,755.5 ns |       6.34 ns |       3.32 ns |  1.08 |    0.00 | 0.0248 |     432 B |        1.10 |
|                          |            |                |             |             |                  |                  |               |               |       |         |        |           |             |
| AcquireAndReleaseLock    | MediumRun  | 15             | 2           | 10          | ?                |       1,615.3 ns |       6.77 ns |       9.48 ns |  1.00 |    0.01 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | MediumRun  | 15             | 2           | 10          | ?                |       1,670.3 ns |       9.10 ns |      13.33 ns |  1.03 |    0.01 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | MediumRun  | 15             | 2           | 10          | ?                |         765.3 ns |       4.74 ns |       6.94 ns |  0.47 |    0.01 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | MediumRun  | 15             | 2           | 10          | ?                |       1,686.2 ns |      14.02 ns |      20.10 ns |  1.04 |    0.01 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | MediumRun  | 15             | 2           | 10          | ?                |       1,749.9 ns |       3.83 ns |       5.25 ns |  1.08 |    0.01 | 0.0248 |     432 B |        1.10 |
|                          |            |                |             |             |                  |                  |               |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               |  **47,562,807.4 ns** | **431,178.43 ns** | **256,587.36 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |             |                  |                  |               |               |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 10               |  47,526,820.6 ns | 181,513.46 ns | 271,680.82 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |             |                  |                  |               |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **102,766,376.4 ns** | **983,732.02 ns** | **650,677.82 ns** |     **?** |       **?** |      **-** |  **190645 B** |           **?** |
|                          |            |                |             |             |                  |                  |               |               |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 50               | 102,502,656.8 ns | 483,081.50 ns | 723,053.70 ns |     ? |       ? |      - |  191923 B |           ? |
|                          |            |                |             |             |                  |                  |               |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **100**              | **102,802,691.7 ns** | **724,502.97 ns** | **431,140.09 ns** |     **?** |       **?** |      **-** |  **418803 B** |           **?** |
|                          |            |                |             |             |                  |                  |               |               |       |         |        |           |             |
| ConcurrentLockContention | MediumRun  | 15             | 2           | 10          | 100              | 102,798,151.3 ns | 483,802.11 ns | 724,132.28 ns |     ? |       ? |      - |  416518 B |           ? |
