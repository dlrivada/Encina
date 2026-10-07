```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean             | Error           | StdDev        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |-----------------:|----------------:|--------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |         **951.1 ns** |        **12.52 ns** |       **6.55 ns** |  **1.00** |    **0.01** | **0.0038** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       1,011.6 ns |         4.11 ns |       2.45 ns |  1.06 |    0.01 | 0.0038 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |         407.5 ns |         9.42 ns |       5.60 ns |  0.43 |    0.01 | 0.0010 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       1,013.2 ns |        40.81 ns |      26.99 ns |  1.07 |    0.03 | 0.0038 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       1,110.7 ns |        96.02 ns |      63.51 ns |  1.17 |    0.06 | 0.0038 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |         961.2 ns |       269.38 ns |      14.77 ns |  1.00 |    0.02 | 0.0038 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |         996.2 ns |        53.95 ns |       2.96 ns |  1.04 |    0.01 | 0.0038 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |         411.5 ns |       155.74 ns |       8.54 ns |  0.43 |    0.01 | 0.0010 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       1,017.0 ns |       464.78 ns |      25.48 ns |  1.06 |    0.03 | 0.0038 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       1,118.4 ns |       993.08 ns |      54.43 ns |  1.16 |    0.05 | 0.0038 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **46,601,188.1 ns** |   **885,563.16 ns** | **585,745.20 ns** |     **?** |       **?** |      **-** |   **17682 B** |           **?** |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  47,445,636.8 ns | 2,893,038.72 ns | 158,577.21 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **103,029,343.2 ns** |   **676,667.11 ns** | **447,573.40 ns** |     **?** |       **?** |      **-** |  **191904 B** |           **?** |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 103,074,503.9 ns | 8,687,457.68 ns | 476,188.87 ns |     ? |       ? |      - |  191904 B |           ? |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **103,302,630.8 ns** |   **901,339.66 ns** | **596,180.37 ns** |     **?** |       **?** |      **-** |  **419504 B** |           **?** |
|                          |            |                |             |                  |                  |                 |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 102,773,282.9 ns | 6,165,251.47 ns | 337,938.24 ns |     ? |       ? |      - |  416245 B |           ? |
