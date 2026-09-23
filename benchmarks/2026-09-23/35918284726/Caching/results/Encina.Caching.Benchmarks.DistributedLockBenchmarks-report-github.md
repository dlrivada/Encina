```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean             | Error            | StdDev        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |-----------------:|-----------------:|--------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **1,784.2 ns** |          **1.28 ns** |       **0.67 ns** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       1,862.5 ns |          7.75 ns |       4.61 ns |  1.04 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |         776.1 ns |          1.81 ns |       1.08 ns |  0.43 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       1,880.2 ns |          4.03 ns |       2.40 ns |  1.05 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       1,944.4 ns |          9.81 ns |       5.83 ns |  1.09 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       1,802.1 ns |         27.70 ns |       1.52 ns |  1.00 |    0.00 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       1,853.5 ns |         38.40 ns |       2.10 ns |  1.03 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |         780.4 ns |         22.36 ns |       1.23 ns |  0.43 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       1,862.9 ns |         33.88 ns |       1.86 ns |  1.03 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       1,952.4 ns |         54.65 ns |       3.00 ns |  1.08 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **47,205,118.3 ns** |    **383,693.04 ns** | **253,789.19 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  46,643,471.7 ns | 12,146,244.77 ns | 665,776.66 ns |     ? |       ? |      - |   17846 B |           ? |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **101,780,261.7 ns** |    **553,914.91 ns** | **329,625.88 ns** |     **?** |       **?** |      **-** |  **191904 B** |           **?** |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 102,518,386.0 ns |  6,385,998.03 ns | 350,038.10 ns |     ? |       ? |      - |  189323 B |           ? |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **102,966,795.9 ns** |    **691,134.17 ns** | **457,142.46 ns** |     **?** |       **?** |      **-** |  **419504 B** |           **?** |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 102,277,302.0 ns |  5,849,441.99 ns | 320,627.66 ns |     ? |       ? |      - |  419504 B |           ? |
