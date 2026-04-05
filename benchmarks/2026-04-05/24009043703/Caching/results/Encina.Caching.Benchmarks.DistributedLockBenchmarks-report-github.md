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
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **?**                |       **1,785.4 ns** |       **7.00 ns** |       **4.17 ns** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |       1,846.5 ns |       4.63 ns |       2.42 ns |  1.03 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |         774.3 ns |       2.00 ns |       1.19 ns |  0.43 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |       1,873.7 ns |       4.42 ns |       2.93 ns |  1.05 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |       1,952.0 ns |      10.67 ns |       6.35 ns |  1.09 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| AcquireAndReleaseLock    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |   9,698,131.0 ns |            NA |       0.00 ns |  1.00 |    0.00 |      - |     392 B |        1.00 |
| TryAcquireAsync_Success  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |  10,145,406.0 ns |            NA |       0.00 ns |  1.05 |    0.00 |      - |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |   4,742,167.0 ns |            NA |       0.00 ns |  0.49 |    0.00 |      - |     104 B |        0.27 |
| IsLockedAsync_Locked     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |   9,951,084.0 ns |            NA |       0.00 ns |  1.03 |    0.00 |      - |     400 B |        1.02 |
| ExtendLock               | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |  11,056,348.0 ns |            NA |       0.00 ns |  1.14 |    0.00 |      - |     432 B |        1.10 |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **10**               |  **47,487,549.7 ns** | **361,132.88 ns** | **238,867.04 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| ConcurrentLockContention | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10               |  52,125,469.0 ns |            NA |       0.00 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **50**               | **102,755,713.2 ns** | **871,386.87 ns** | **518,548.35 ns** |     **?** |       **?** |      **-** |  **191904 B** |           **?** |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| ConcurrentLockContention | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 50               | 122,937,414.0 ns |            NA |       0.00 ns |     ? |       ? |      - |  191904 B |           ? |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **100**              | **103,228,524.2 ns** | **732,284.82 ns** | **484,361.07 ns** |     **?** |       **?** |      **-** |  **406467 B** |           **?** |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| ConcurrentLockContention | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 100              | 118,048,195.0 ns |            NA |       0.00 ns |     ? |       ? |      - |  403208 B |           ? |
