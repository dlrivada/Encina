```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean             | Error           | StdDev          | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |----------------- |-----------------:|----------------:|----------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |       **1,915.3 ns** |         **7.53 ns** |         **4.98 ns** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | ?                |       1,950.9 ns |         9.54 ns |         6.31 ns |  1.02 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | ?                |         839.8 ns |         2.28 ns |         1.51 ns |  0.44 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | ?                |       2,009.8 ns |        11.99 ns |         7.93 ns |  1.05 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | ?                |       2,072.8 ns |         8.63 ns |         4.51 ns |  1.08 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| AcquireAndReleaseLock    | ShortRun   | 3              | 1           | ?                |       1,916.3 ns |        19.82 ns |         1.09 ns |  1.00 |    0.00 | 0.0229 |     392 B |        1.00 |
| TryAcquireAsync_Success  | ShortRun   | 3              | 1           | ?                |       1,957.2 ns |       226.90 ns |        12.44 ns |  1.02 |    0.01 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | ShortRun   | 3              | 1           | ?                |         827.6 ns |        62.65 ns |         3.43 ns |  0.43 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | ShortRun   | 3              | 1           | ?                |       2,041.6 ns |       134.60 ns |         7.38 ns |  1.07 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | ShortRun   | 3              | 1           | ?                |       2,069.2 ns |        39.13 ns |         2.14 ns |  1.08 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **52,997,369.6 ns** | **3,999,776.08 ns** | **2,645,604.23 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 10               |  45,934,147.9 ns | 8,538,309.78 ns |   468,013.57 ns |     ? |       ? |      - |   17564 B |           ? |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **101,490,760.4 ns** |   **273,845.95 ns** |   **181,132.14 ns** |     **?** |       **?** |      **-** |  **186221 B** |           **?** |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 50               | 101,484,279.2 ns | 6,932,034.36 ns |   379,968.20 ns |     ? |       ? |      - |  193314 B |           ? |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **101,352,857.2 ns** |   **502,056.32 ns** |   **332,079.17 ns** |     **?** |       **?** |      **-** |  **429101 B** |           **?** |
|                          |            |                |             |                  |                  |                 |                 |       |         |        |           |             |
| ConcurrentLockContention | ShortRun   | 3              | 1           | 100              | 101,504,235.8 ns | 3,802,273.59 ns |   208,415.45 ns |     ? |       ? |      - |  415861 B |           ? |
