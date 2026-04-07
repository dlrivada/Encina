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
| **AcquireAndReleaseLock**    | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **?**                |       **1,769.5 ns** |       **8.48 ns** |       **5.61 ns** |  **1.00** |    **0.00** | **0.0229** |     **392 B** |        **1.00** |
| TryAcquireAsync_Success  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |       1,882.4 ns |       4.35 ns |       2.59 ns |  1.06 |    0.00 | 0.0229 |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |         769.9 ns |       0.88 ns |       0.52 ns |  0.44 |    0.00 | 0.0057 |     104 B |        0.27 |
| IsLockedAsync_Locked     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |       1,852.2 ns |       1.86 ns |       1.11 ns |  1.05 |    0.00 | 0.0229 |     400 B |        1.02 |
| ExtendLock               | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |       2,024.2 ns |       6.43 ns |       4.25 ns |  1.14 |    0.00 | 0.0229 |     432 B |        1.10 |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| AcquireAndReleaseLock    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |   9,355,188.0 ns |            NA |       0.00 ns |  1.00 |    0.00 |      - |     392 B |        1.00 |
| TryAcquireAsync_Success  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |   9,401,995.0 ns |            NA |       0.00 ns |  1.01 |    0.00 |      - |     392 B |        1.00 |
| IsLockedAsync_NotLocked  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |   4,704,944.0 ns |            NA |       0.00 ns |  0.50 |    0.00 |      - |     104 B |        0.27 |
| IsLockedAsync_Locked     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |   9,785,897.0 ns |            NA |       0.00 ns |  1.05 |    0.00 |      - |     400 B |        1.02 |
| ExtendLock               | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                |  10,515,987.0 ns |            NA |       0.00 ns |  1.12 |    0.00 |      - |     432 B |        1.10 |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **10**               |  **47,335,152.4 ns** | **485,581.07 ns** | **321,181.81 ns** |     **?** |       **?** |      **-** |   **17864 B** |           **?** |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| ConcurrentLockContention | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10               |  49,270,025.0 ns |            NA |       0.00 ns |     ? |       ? |      - |   17864 B |           ? |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **50**               | **102,408,363.2 ns** | **914,010.35 ns** | **604,561.25 ns** |     **?** |       **?** |      **-** |  **191904 B** |           **?** |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| ConcurrentLockContention | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 50               | 118,825,144.0 ns |            NA |       0.00 ns |     ? |       ? |      - |  192968 B |           ? |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| **ConcurrentLockContention** | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **100**              | **102,583,750.3 ns** | **755,943.67 ns** | **500,009.93 ns** |     **?** |       **?** |      **-** |  **419504 B** |           **?** |
|                          |            |                |             |             |              |             |                  |                  |               |               |       |         |        |           |             |
| ConcurrentLockContention | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 100              | 120,645,053.0 ns |            NA |       0.00 ns |     ? |       ? |      - |  419504 B |           ? |
