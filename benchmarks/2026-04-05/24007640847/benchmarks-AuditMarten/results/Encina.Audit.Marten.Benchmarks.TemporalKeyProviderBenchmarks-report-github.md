```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                 | PeriodCount | Mean      | Error | Ratio | Rank | Allocated | Alloc Ratio |
|----------------------- |------------ |----------:|------:|------:|-----:|----------:|------------:|
| **GetExistingKey**         | **12**          | **15.127 ms** |    **NA** |  **1.00** |    **5** |     **352 B** |        **1.00** |
| GetOrCreateExistingKey | 12          |  6.300 ms |    NA |  0.42 |    1 |     328 B |        0.93 |
| CreateNewKey           | 12          |  7.643 ms |    NA |  0.51 |    2 |     848 B |        2.41 |
| IsKeyDestroyed         | 12          | 10.880 ms |    NA |  0.72 |    3 |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 11.514 ms |    NA |  0.76 |    4 |    2464 B |        7.00 |
|                        |             |           |       |       |      |           |             |
| **GetExistingKey**         | **84**          | **15.202 ms** |    **NA** |  **1.00** |    **5** |     **352 B** |        **1.00** |
| GetOrCreateExistingKey | 84          |  6.353 ms |    NA |  0.42 |    1 |     328 B |        0.93 |
| CreateNewKey           | 84          |  7.706 ms |    NA |  0.51 |    2 |     848 B |        2.41 |
| IsKeyDestroyed         | 84          | 10.506 ms |    NA |  0.69 |    3 |     112 B |        0.32 |
| GetActiveKeysCount     | 84          | 11.619 ms |    NA |  0.76 |    4 |   15272 B |       43.39 |
