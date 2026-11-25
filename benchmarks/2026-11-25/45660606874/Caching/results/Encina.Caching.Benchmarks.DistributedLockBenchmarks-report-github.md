```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean             | Error            | StdDev        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |-----------------:|-----------------:|--------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **1,238.3 ns** |          **8.60 ns** |       **5.69 ns** |  **1.00** |    **0.01** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       1,231.3 ns |         12.26 ns |       8.11 ns |  0.99 |    0.01 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |         513.8 ns |          2.68 ns |       1.40 ns |  0.41 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       1,272.8 ns |         11.84 ns |       7.05 ns |  1.03 |    0.01 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       1,327.7 ns |         15.84 ns |      10.48 ns |  1.07 |    0.01 | 0.0248 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       1,221.0 ns |        576.04 ns |      31.57 ns |  1.00 |    0.03 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       1,259.4 ns |        185.43 ns |      10.16 ns |  1.03 |    0.02 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |         530.4 ns |         26.33 ns |       1.44 ns |  0.43 |    0.01 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       1,332.1 ns |        544.44 ns |      29.84 ns |  1.09 |    0.03 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       1,354.1 ns |        220.23 ns |      12.07 ns |  1.11 |    0.03 | 0.0248 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **50,131,380.8 ns** |  **1,277,844.99 ns** | **845,215.34 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  48,926,452.1 ns | 13,288,348.76 ns | 728,379.24 ns |     ? |       ? |      - |   17719 B |           ? |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **101,308,622.0 ns** |    **354,325.07 ns** | **234,364.10 ns** |     **?** |       **?** |      **-** |  **189784 B** |           **?** |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 101,120,030.2 ns |  7,034,343.29 ns | 385,576.09 ns |     ? |       ? |      - |  192864 B |           ? |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **101,584,000.7 ns** |    **459,165.87 ns** | **303,709.79 ns** |     **?** |       **?** |      **-** |  **432739 B** |           **?** |
|                          |            |                |             |                  |                  |                  |               |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 101,531,322.9 ns |  8,274,016.30 ns | 453,526.75 ns |     ? |       ? |      - |  429392 B |           ? |
