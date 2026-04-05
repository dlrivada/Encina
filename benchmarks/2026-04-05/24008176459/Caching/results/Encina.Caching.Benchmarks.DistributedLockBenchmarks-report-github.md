```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | concurrencyLevel | Mean             | Error         | StdDev        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------- |-----------------:|--------------:|--------------:|------:|--------:|-------:|----------:|------------:|
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **?**                |       **1,808.7 ns** |       **8.53 ns** |       **5.08 ns** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |       1,842.8 ns |       6.70 ns |       4.43 ns |  1.02 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |         775.1 ns |       0.98 ns |       0.51 ns |  0.43 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |       1,854.9 ns |       6.00 ns |       3.57 ns |  1.03 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |       1,939.2 ns |       7.90 ns |       4.70 ns |  1.07 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| AcquireAndReleaseLock    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |   9,509,175.0 ns |            NA |       0.00 ns |  1.00 |    0.00 |      - |     392 B |        1.00 |
| TryAcquireAsync_Success  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |   9,416,421.0 ns |            NA |       0.00 ns |  0.99 |    0.00 |      - |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |   4,529,135.0 ns |            NA |       0.00 ns |  0.48 |    0.00 |      - |     104 B |        0.27 |
| IsLockedAsync_Locked     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |   9,781,675.0 ns |            NA |       0.00 ns |  1.03 |    0.00 |      - |     400 B |        1.02 |
| ExtendLock               | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |  10,649,470.0 ns |            NA |       0.00 ns |  1.12 |    0.00 |      - |     432 B |        1.10 |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **10**               |  **47,121,721.6 ns** | **385,149.44 ns** | **254,752.51 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| ConcurrentLockContention | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10               |  63,345,691.0 ns |            NA |       0.00 ns |     ? |       ? |      - |   18928 B |           ? |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **50**               | **102,388,858.5 ns** | **838,704.47 ns** | **554,751.08 ns** |     **?** |       **?** |      **-** |  **186856 B** |           **?** |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| ConcurrentLockContention | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 50               | 120,028,212.0 ns |            NA |       0.00 ns |     ? |       ? |      - |  192968 B |           ? |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **100**              | **102,817,778.8 ns** | **437,726.92 ns** | **228,939.64 ns** |     **?** |       **?** |      **-** |  **419504 B** |           **?** |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| ConcurrentLockContention | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 100              | 118,580,357.0 ns |            NA |       0.00 ns |     ? |       ? |      - |  410480 B |           ? |
