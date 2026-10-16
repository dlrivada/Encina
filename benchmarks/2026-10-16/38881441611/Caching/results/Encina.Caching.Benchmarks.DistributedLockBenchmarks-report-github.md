```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean             | Error            | StdDev        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |-----------------:|-----------------:|--------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **1,818.2 ns** |          **3.28 ns** |       **2.17 ns** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       1,902.0 ns |          4.50 ns |       2.68 ns |  1.05 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |         790.2 ns |          1.02 ns |       0.53 ns |  0.43 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       1,892.0 ns |          3.12 ns |       1.85 ns |  1.04 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       1,979.1 ns |          7.44 ns |       4.92 ns |  1.09 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       1,815.9 ns |         22.16 ns |       1.21 ns |  1.00 |    0.00 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       1,887.3 ns |        110.20 ns |       6.04 ns |  1.04 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |         781.7 ns |         24.31 ns |       1.33 ns |  0.43 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       1,956.3 ns |         95.38 ns |       5.23 ns |  1.08 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       1,985.5 ns |         95.42 ns |       5.23 ns |  1.09 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **46,795,590.5 ns** |  **1,140,103.73 ns** | **754,108.03 ns** |     **?** |       **?** |      **-** |   **17444 B** |           **?** |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  47,191,961.4 ns |  3,690,169.80 ns | 202,270.66 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **102,668,126.7 ns** |    **604,656.35 ns** | **399,942.74 ns** |     **?** |       **?** |      **-** |  **191178 B** |           **?** |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 103,400,780.3 ns | 13,279,220.08 ns | 727,878.86 ns |     ? |       ? |      - |  191923 B |           ? |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **102,622,031.7 ns** |    **786,914.13 ns** | **520,494.97 ns** |     **?** |       **?** |      **-** |  **416245 B** |           **?** |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 102,509,080.3 ns | 13,238,606.26 ns | 725,652.68 ns |     ? |       ? |      - |  419203 B |           ? |
